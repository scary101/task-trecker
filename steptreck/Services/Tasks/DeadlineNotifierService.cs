using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;
using steptreck.API.Services.Notifications;

namespace steptreck.API.Services.Tasks
{
    public class DeadlineNotifierService : BackgroundService
    {
        private static readonly int[] ThresholdsHours = [24, 12, 6, 3, 1];
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeadlineNotifierService> _logger;

        public DeadlineNotifierService(
            IServiceScopeFactory scopeFactory,
            ILogger<DeadlineNotifierService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private static DateTime UtcNow() => DateTime.UtcNow;

        private static DateTime UtcNowTimestamp() => DateTime.UtcNow;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var period = TimeSpan.FromMinutes(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Tick(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deadline notifier tick failed.");
                }

                await Task.Delay(period, stoppingToken);
            }
        }

        private async Task Tick(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<NotificationsService>();
            var helper = scope.ServiceProvider.GetRequiredService<NotificationsHelper>();
            var scores = scope.ServiceProvider.GetRequiredService<MemberScoreService>();

            var now = UtcNow();
            var updatedAt = UtcNowTimestamp();
            var sentAtUtc = DateTime.UtcNow;

            var maxH = ThresholdsHours.Max();
            var horizon = now.AddHours(maxH);

            var candidates = await db.ProjectTasks
                .AsNoTracking()
                .Where(t => t.Deadline != null
                            && t.Deadline > now
                            && t.Deadline <= horizon
                            && !t.IsDone
                            && t.Status != "Done"
                            && !t.IsArchived
                            && t.AssignedToMemberId != null)
                .Select(t => new
                {
                    TaskId = t.Id,
                    t.Title,
                    DeadlineUtc = t.Deadline!.Value,
                    AssigneeMemberId = t.AssignedToMemberId!.Value
                })
                .ToListAsync(ct);

            if (candidates.Count > 0)
            {
                var taskIds = candidates.Select(x => x.TaskId).Distinct().ToList();

                var sent = await db.Set<TaskDeadlineNotification>()
                    .AsNoTracking()
                    .Where(x => taskIds.Contains(x.TaskId))
                    .Select(x => new { x.TaskId, x.MemberId, x.HoursBefore })
                    .ToListAsync(ct);

                var sentSet = new HashSet<(int taskId, int memberId, short hours)>(
                    sent.Select(x => (x.TaskId, x.MemberId, x.HoursBefore)));

                var toInsert = new List<TaskDeadlineNotification>();

                const double windowMinutes = 2;

                foreach (var t in candidates)
                {
                    var minutesLeft = (t.DeadlineUtc - now).TotalMinutes;
                    if (minutesLeft <= 0) continue;

                    foreach (var h in ThresholdsHours)
                    {
                        var thresholdMinutes = h * 60.0;

                        var inWindow = minutesLeft <= thresholdMinutes
                                       && minutesLeft > thresholdMinutes - windowMinutes;

                        if (!inWindow) continue;

                        var hh = (short)h;

                        if (sentSet.Contains((t.TaskId, t.AssigneeMemberId, hh)))
                            continue;

                        var text = helper.DeadlineTime(t.Title, h);
                        await notifications.CreateForMember(t.AssigneeMemberId, text, ct);

                        toInsert.Add(new TaskDeadlineNotification
                        {
                            TaskId = t.TaskId,
                            MemberId = t.AssigneeMemberId,
                            HoursBefore = hh,
                            SentAtUtc = sentAtUtc
                        });

                        sentSet.Add((t.TaskId, t.AssigneeMemberId, hh));
                    }
                }

                if (toInsert.Count > 0)
                {
                    db.AddRange(toInsert);
                    await db.SaveChangesAsync(ct);
                }
            }

            var overdue = await db.ProjectTasks
                .Where(t => t.Deadline != null
                            && t.Deadline <= now
                            && !t.IsDone
                            && t.Status != "Done"
                            && !t.IsArchived
                            && !t.IsMissed
                            && t.AssignedToMemberId != null)
                .Select(t => new
                {
                    TaskId = t.Id,
                    t.Title,
                    AssigneeMemberId = t.AssignedToMemberId!.Value,
                    t.PriorityId
                })
                .ToListAsync(ct);

            if (overdue.Count == 0) return;

            var overdueIds = overdue.Select(x => x.TaskId).ToList();

            var entities = await db.ProjectTasks
                .Where(t => overdueIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var e in entities)
            {
                e.IsMissed = true;
                e.UpdatedAt = updatedAt;

                if (e.AssignedToMemberId.HasValue)
                    await scores.RegisterMissedAsync(e.AssignedToMemberId.Value, e.PriorityId, ct);
            }

            await db.SaveChangesAsync(ct);

            foreach (var x in overdue)
            {
                var text = $"Вы просрочили дедлайн по задаче «{x.Title}». Уровень доверия был снижен.";
                await notifications.CreateForMember(x.AssigneeMemberId, text, ct);
            }
        }
    }
}
