using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.API.Services.Notifications;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.TaskDTOs;
using steptreck.Domain.Enums;
using System.Reactive;
using System.Threading.Tasks;

namespace steptreck.API.Services.Tasks
{
    public class TaskService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly NotificationsService _notificationServise;
        private readonly NotificationsHelper _notificationHelper;
        private readonly MemberScoreService _memberScoreService;
        private readonly AuditService _auditService;


        public TaskService(
            AppDbContext context,
            UserHelper userHelper,
            NotificationsService notificationServise,
            NotificationsHelper notificationHelper,
            MemberScoreService memberScoreService,
            AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _notificationServise = notificationServise;
            _notificationHelper = notificationHelper;
            _memberScoreService = memberScoreService;
            _auditService = auditService;
        }

        private static DateTime UtcNowTimestamp() => DateTime.UtcNow;

        private static DateTime UtcNow() => DateTime.UtcNow;

        private async Task EnsureCurrentUserIsNotOwnerAsync(CancellationToken ct)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            var roleId = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.UserId == userId && m.IsActive)
                .Select(m => (int?)m.RoleId)
                .FirstOrDefaultAsync(ct);

            if (roleId == (int)RoleEnum.Owner)
                throw new UnauthorizedAccessException("Руководителю недоступны задачи");
        }

        private static DateTime ToUtcTimestamp(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
                _ => value
            };

            return utc;
        }

        public async Task<int> CreateTaskForLead(CreateTaskForTeamDto model, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            int memberId = await _context.Members
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => m.Id)
                .FirstAsync(ct);

            var team = await _context.ProjectTeams
                .AsNoTracking()
                .Where(t =>
                    t.Id == model.TeamId &&
                    t.ProjectId == model.ProjectId &&
                    t.Project.OrganizationId == orgId &&
                    t.IsActive &&
                    !t.Project.IsArchived)
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync(ct);

            if (team is null)
                throw new InvalidOperationException("Команда или проект недоступны");

            if (model.AssignedToMemberId.HasValue)
            {
                var assigneeInTeam = await _context.ProjectTeamMembers
                    .AnyAsync(ptm =>
                        ptm.TeamId == model.TeamId &&
                        ptm.MemberId == model.AssignedToMemberId.Value &&
                        ptm.Member.OrganizationId == orgId &&
                        ptm.Member.IsActive, ct);

                if (!assigneeInTeam)
                    throw new InvalidOperationException("Ответственный не состоит в этой команде");
            }

            DateTime? deadlineUtc = null;

            if (model.Deadline.HasValue)
            {
                var d = model.Deadline.Value;

                deadlineUtc = ToUtcTimestamp(d);
            }
            var priorityId = (int)model.Priority;
            var priority = await _context.TaskPriorities
                .Where(p => p.Id == priorityId)
                .Select(p => new { p.Id, p.Title, p.Code, p.IsActive })
                .FirstOrDefaultAsync(ct);

            if (priority == null || !priority.IsActive)
                throw new InvalidOperationException("Недопустимый приоритет");

            var task = new ProjectTask
            {
                ProjectId = model.ProjectId,
                TeamId = model.TeamId,
                Title = model.Title,
                Description = model.Description,
                Deadline = deadlineUtc,
                Priority = string.IsNullOrWhiteSpace(priority.Title) ? priority.Code : priority.Title,
                PriorityId = priority.Id,
                Status = TaskStatusEnum.Todo.ToString(),
                CreatedAt = UtcNowTimestamp(),
                CreatedByMemberId = memberId,
                AssignedToMemberId = model.AssignedToMemberId,
            };

            if (model.AssignedToMemberId.HasValue)
                await _memberScoreService.RegisterAssignedAsync(model.AssignedToMemberId.Value, ct);

            if (model.Checklist != null)
            {
                for (int i = 0; i < model.Checklist.Count; i++)
                {
                    var title = model.Checklist[i].Title?.Trim();
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    task.ProjectTaskChecklistItems.Add(new ProjectTaskChecklistItem
                    {
                        Title = title,
                        IsDone = false,
                        SortOrder = i + 1,
                        CreatedAt = UtcNow()
                    });
                }
            }

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, memberId, new AuditLogCreateDto
            {
                ProjectId = task.ProjectId,
                TeamId = task.TeamId,
                TaskId = task.Id,
                TargetMemberId = task.AssignedToMemberId,
                Action = "create",
                EntityType = "project_task",
                EntityId = task.Id,
                EntityName = task.Title,
                Title = "Создана задача",
                NewValues = new
                {
                    task.Title,
                    task.Description,
                    task.Deadline,
                    task.Priority,
                    task.AssignedToMemberId
                }
            }, ct);

            if (model.AssignedToMemberId.HasValue)
            {
                var text = _notificationHelper.TaskCreated(model, priority.Title);

                await _notificationServise.CreateForMember(
                    model.AssignedToMemberId.Value,
                    text,
                    ct);
            }


            return task.Id;
        }

        public async Task<TaskReadDto?> GetTaskByIdAsync(int taskId, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var task = await _context.ProjectTasks
                .Where(t =>
                    t.Id == taskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive)
                )
                .Select(t => new TaskReadDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    TeamId = t.TeamId,

                    Title = t.Title,
                    Description = t.Description,

                    Status = t.Status,
                    Priority = t.PriorityNavigation.Title,

                    Deadline = t.Deadline,
                    IsArchived = t.IsArchived,

                    CreatedBy =
                        t.CreatedByMember.Surname + " " +
                        t.CreatedByMember.Name +
                        (t.CreatedByMember.Patronymic == null
                            ? ""
                            : " " + t.CreatedByMember.Patronymic),

                    AssignedTo = t.AssignedToMember == null
                        ? null
                        : t.AssignedToMember.Surname + " " +
                          t.AssignedToMember.Name +
                          (t.AssignedToMember.Patronymic == null
                              ? ""
                              : " " + t.AssignedToMember.Patronymic),

                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,

                    Checklist = t.ProjectTaskChecklistItems
                        .OrderBy(c => c.SortOrder)
                        .Select(c => new TaskChecklistItemDto
                        {
                            Id = c.Id,
                            Title = c.Title,
                            IsDone = c.IsDone,
                            SortOrder = c.SortOrder,
                            CreatedAt = c.CreatedAt,
                            CompletedAt = c.CompletedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            return task;
        }

        public async Task CompleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var task = await _context.ProjectTasks
                .Where(t =>
                    t.Id == taskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived)
                .FirstOrDefaultAsync(ct);

            if (task == null)
                throw new InvalidOperationException("Таска не найдена");

            var now = UtcNowTimestamp();

            if (task.Status == "Done" && task.IsDone)
                return;

            bool hasUndoneChecklistItems = await _context.ProjectTaskChecklistItems
                .AnyAsync(i => i.TaskId == taskId && !i.IsDone, ct);

            if (hasUndoneChecklistItems)
                throw new InvalidOperationException(
                    "Нельзя завершить таску, пока не выполнены все пункты чек-листа"
                );

            task.Status = "Done";
            task.IsDone = true;
            task.UpdatedAt = now;
            task.CompletedAt = now;

            if (task.AssignedToMemberId.HasValue)
            {
                if (task.Deadline.HasValue && task.Deadline.Value <= now && !task.IsMissed)
                {
                    task.IsMissed = true;
                    await _memberScoreService.RegisterMissedAsync(task.AssignedToMemberId.Value, task.PriorityId, ct);
                }
                else if (!task.IsMissed)
                {
                    await _memberScoreService.RegisterCompletedAsync(task.AssignedToMemberId.Value, task.PriorityId, ct);
                }
            }

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = task.ProjectId,
                TeamId = task.TeamId,
                TaskId = task.Id,
                TargetMemberId = task.AssignedToMemberId,
                Action = "update",
                EntityType = "project_task",
                EntityId = task.Id,
                EntityName = task.Title,
                Title = "Задача завершена",
                NewValues = new { task.Status, task.IsDone, task.CompletedAt, task.IsMissed }
            }, ct);

            if (task.AssignedToMemberId.HasValue)
            {
                var text = _notificationHelper.TaskCompleted(task.Title);
                await _notificationServise.CreateForMember(task.AssignedToMemberId.Value, text, ct);
            }
        }

        public async Task DeleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            var member = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.UserId == userId && m.IsActive)
                .Select(m => new { m.Id, m.RoleId })
                .FirstOrDefaultAsync(ct);

            if (member is null)
                throw new UnauthorizedAccessException("Участник не найден");

            if (member.RoleId != (int)RoleEnum.TeamLead)
                throw new UnauthorizedAccessException("Удалять задачи может только TeamLead");

            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t =>
                    t.Id == taskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    t.TeamId != null &&
                    t.Team!.IsActive &&
                    !t.IsArchived, ct);

            if (task is null)
                throw new InvalidOperationException("Задача не найдена или недоступна");

            var isLeadInTeam = await _context.ProjectTeamMembers
                .AnyAsync(ptm =>
                    ptm.TeamId == task.TeamId.GetValueOrDefault() &&
                    ptm.MemberId == member.Id &&
                    ptm.Team.IsActive &&
                    !ptm.Team.Project.IsArchived, ct);

            if (!isLeadInTeam)
                throw new UnauthorizedAccessException("Удалять можно только задачи своей команды");

            task.IsArchived = true;
            task.UpdatedAt = UtcNowTimestamp();

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, member.Id, new AuditLogCreateDto
            {
                ProjectId = task.ProjectId,
                TeamId = task.TeamId,
                TaskId = task.Id,
                TargetMemberId = task.AssignedToMemberId,
                Action = "delete",
                EntityType = "project_task",
                EntityId = task.Id,
                EntityName = task.Title,
                Title = "Задача удалена",
                OldValues = new { IsArchived = false },
                NewValues = new { task.IsArchived }
            }, ct);
        }

        public async Task<List<TaskListItemDto>> GetTeamTasksAsync(int teamId, TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var query = _context.ProjectTasks
                .AsNoTracking()
                .Where(t =>
                    t.TeamId == teamId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    t.Team != null &&
                    t.Team.IsActive &&
                    !t.IsArchived
                );

            if (!string.IsNullOrWhiteSpace(filter?.Status))
            {
                var status = filter!.Status.Trim();
                query = query.Where(t => t.Status == status);
            }
            if (filter?.DateFrom != null)
            {
                var dateFromUtc = ToUtcTimestamp(filter.DateFrom.Value);
                query = query.Where(t => t.Deadline != null && t.Deadline >= dateFromUtc);
            }

            if (filter?.DateTo != null)
            {
                var dateToUtc = ToUtcTimestamp(filter.DateTo.Value);
                query = query.Where(t => t.Deadline != null && t.Deadline <= dateToUtc);
            }

            return await query
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItemDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.PriorityNavigation.Title,
                    Deadline = t.Deadline,

                    ProjectId = t.ProjectId,
                    TeamId = t.TeamId,
                    TeamName = t.Team != null ? t.Team.Name : null,

                    AssignedTo = t.AssignedToMember == null
                        ? null
                        : t.AssignedToMember.Surname + " " +
                          t.AssignedToMember.Name +
                          (t.AssignedToMember.Patronymic == null ? "" : " " + t.AssignedToMember.Patronymic)
                })
                .ToListAsync(ct);
        }

        public async Task<List<TaskListItemDto>> GetMemberTasksAsync(int memberId, TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var query = _context.ProjectTasks
                .AsNoTracking()
                .Where(t =>
                    t.AssignedToMemberId == memberId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived
                );

            if (!string.IsNullOrWhiteSpace(filter?.Status))
            {
                var status = filter!.Status.Trim();
                query = query.Where(t => t.Status == status);
            }

            if (filter?.DateFrom != null)
            {
                var dateFromUtc = ToUtcTimestamp(filter.DateFrom.Value);
                query = query.Where(t => t.Deadline != null && t.Deadline >= dateFromUtc);
            }

            if (filter?.DateTo != null)
            {
                var dateToUtc = ToUtcTimestamp(filter.DateTo.Value);
                query = query.Where(t => t.Deadline != null && t.Deadline <= dateToUtc);
            }

            return await query
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new TaskListItemDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.PriorityNavigation.Title,
                    Deadline = t.Deadline,

                    ProjectId = t.ProjectId,
                    TeamId = t.TeamId,
                    TeamName = t.Team != null ? t.Team.Name : null,

                    AssignedTo = t.AssignedToMember == null
                        ? null
                        : t.AssignedToMember.Surname + " " +
                          t.AssignedToMember.Name +
                          (t.AssignedToMember.Patronymic == null ? "" : " " + t.AssignedToMember.Patronymic)
                })
                .ToListAsync(ct);
        }
        public async Task<List<TaskListItemDto>> GetMyTasksAsync(TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            int memberId = await _context.Members
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => m.Id)
                .FirstAsync(ct);

            return await GetMemberTasksAsync(memberId, filter, ct);
        }
        public async Task PutDeadLine(PutDeadLineDto model, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var info = await _context.ProjectTasks
                .Where(t =>
                    t.Id == model.TaskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived)
                .Select(t => new { t.Title, t.AssignedToMemberId })
                .FirstOrDefaultAsync(ct);

            if (info == null)
                throw new InvalidOperationException("Задача не найдена");

            var d = model.Date;
            var deadlineUtc = ToUtcTimestamp(d);
            var now = UtcNowTimestamp();

            var updated = await _context.ProjectTasks
                .Where(t =>
                    t.Id == model.TaskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Deadline, deadlineUtc)
                    .SetProperty(t => t.UpdatedAt, now), ct);

            if (updated == 0)
                throw new InvalidOperationException("Задача не найдена");

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TaskId = model.TaskId,
                TargetMemberId = info.AssignedToMemberId,
                Action = "update",
                EntityType = "project_task",
                EntityId = model.TaskId,
                EntityName = info.Title,
                Title = "Изменен дедлайн задачи",
                NewValues = new { Deadline = deadlineUtc }
            }, ct);

            if (info.AssignedToMemberId.HasValue)
            {
                var text = _notificationHelper.TaskDeadlineChanged(info.Title, deadlineUtc);
                await _notificationServise.CreateForMember(info.AssignedToMemberId.Value, text, ct);
            }
        }




    }
}
