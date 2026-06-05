using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.MemberDTOs;

namespace steptreck.API.Services.Tasks
{
    public class MemberScoreService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;

        public MemberScoreService(AppDbContext context, UserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        public async Task<ScoreDto> GetMyScore(CancellationToken ct = default)
        {
            int userId = _userHelper.GetCurrentUserId();

            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId, ct);

            if (member == null)
                throw new InvalidOperationException("Текущий пользователь не является участником организации.");

            var score = await _context.MemberScores
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.MemberId == member.Id, ct);

            var dto = score == null
                ? CreateDefaultScore(member.Id)
                : Map(score);

            dto.LogScore = await GetLogs(member.Id, take: 30, ct);
            dto.PriorityStats = await GetPriorityStats(member.Id, ct);

            return dto;
        }

        public async Task<ScoreDto> GetScoreMember(int memberId, CancellationToken ct = default)
        {
            var memberExists = await _context.Members
                .AsNoTracking()
                .AnyAsync(m => m.Id == memberId, ct);

            if (!memberExists)
                throw new InvalidOperationException("Участник не найден.");

            var score = await _context.MemberScores
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.MemberId == memberId, ct);

            var dto = score == null
                ? CreateDefaultScore(memberId)
                : Map(score);

            dto.LogScore = await GetLogs(memberId, take: 30, ct);
            dto.PriorityStats = await GetPriorityStats(memberId, ct);

            return dto;
        }


        public async Task<List<ScoreRowDto>> GetTeamScores(
            int teamId,
            CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            var members = await _context.ProjectTeamMembers
                .AsNoTracking()
                .Where(x => x.TeamId == teamId)
                .Select(x => new
                {
                    x.Member.Id,
                    FullName =
                    (x.Member.Surname + " " +
                     x.Member.Name + " " +
                     x.Member.Patronymic).Trim()
                })

                .ToListAsync(ct);

            if (members.Count == 0)
                return new List<ScoreRowDto>();

            var memberIds = members.Select(m => m.Id).ToList();

            var scores = await _context.MemberScores
                .AsNoTracking()
                .Where(s => memberIds.Contains(s.MemberId))
                .ToListAsync(ct);

            var scoreMap = scores.ToDictionary(x => x.MemberId);

            var result = new List<ScoreRowDto>();

            foreach (var m in members)
            {
                scoreMap.TryGetValue(m.Id, out var s);

                var completed = s?.CompletedCount ?? 0;
                var missed = s?.MissedCount ?? 0;
                var trust = s?.Trust ?? 100;
                var totalAssigned = s?.TotalAssignedCount ?? completed + missed;

                var total = Math.Max(1, completed + missed);
                var missRate = Math.Round((decimal)missed / total * 100, 2);

                result.Add(new ScoreRowDto
                {
                    MemberId = m.Id,
                    FullName = m.FullName,
                    CompletedCount = completed,
                    MissedCount = missed,
                    TotalAssignedCount = totalAssigned,
                    Trust = trust,
                    MissRate = missRate,
                    TrustLevel = GetTrustLevel(trust)
                });
            }

            return result
                .OrderByDescending(x => x.Trust)
                .ThenByDescending(x => x.CompletedCount)
                .ToList();
        }

        public async Task RegisterAssignedAsync(int memberId, CancellationToken ct = default)
        {
            var score = await GetOrCreateScore(memberId, ct);
            score.TotalAssignedCount += 1;
            score.UpdatedAtUtc = DateTime.UtcNow;
        }

        public async Task RegisterCompletedAsync(int memberId, int priorityId, CancellationToken ct = default)
        {
            var priority = await GetPriority(priorityId, ct);
            var score = await GetOrCreateScore(memberId, ct);

            score.CompletedCount += 1;
            score.Trust = Math.Min(100, score.Trust + priority.DoneReward);
            score.UpdatedAtUtc = DateTime.UtcNow;

            _context.MemberScoreLogs.Add(new MemberScoreLog
            {
                MemberId = memberId,
                Delta = priority.DoneReward,
                IsIncrease = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        public async Task RegisterMissedAsync(int memberId, int priorityId, CancellationToken ct = default)
        {
            var priority = await GetPriority(priorityId, ct);
            var score = await GetOrCreateScore(memberId, ct);

            score.MissedCount += 1;
            score.Trust = Math.Max(0, score.Trust - priority.MissPenalty);
            score.UpdatedAtUtc = DateTime.UtcNow;

            _context.MemberScoreLogs.Add(new MemberScoreLog
            {
                MemberId = memberId,
                Delta = priority.MissPenalty,
                IsIncrease = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        private async Task<MemberScore> GetOrCreateScore(int memberId, CancellationToken ct)
        {
            var score = await _context.MemberScores
                .FirstOrDefaultAsync(s => s.MemberId == memberId, ct);

            if (score != null)
                return score;

            score = new MemberScore
            {
                MemberId = memberId,
                CompletedCount = 0,
                MissedCount = 0,
                TotalAssignedCount = 0,
                Trust = 100,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.MemberScores.Add(score);
            return score;
        }

        private async Task<steptreck.API.Models.TaskPriority> GetPriority(int priorityId, CancellationToken ct)
        {
            var priority = await _context.TaskPriorities
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == priorityId, ct);

            if (priority == null)
                throw new InvalidOperationException("Приоритет задачи не найден.");

            return priority;
        }


        private static ScoreDto CreateDefaultScore(int memberId)
        {
            return new ScoreDto
            {
                MemberId = memberId,
                CompletedCount = 0,
                MissedCount = 0,
                TotalAssignedCount = 0,
                Trust = 100,
                UpdatedAtUtc = DateTime.UtcNow,
                MissRate = 0,
                TrustLevel = GetTrustLevel(100)
            };
        }

        private static ScoreDto Map(MemberScore s)
        {
            var total = Math.Max(1, s.CompletedCount + s.MissedCount);

            var missRate = Math.Round(
                (decimal)s.MissedCount / total * 100, 2);

            return new ScoreDto
            {
                MemberId = s.MemberId,
                CompletedCount = s.CompletedCount,
                MissedCount = s.MissedCount,
                TotalAssignedCount = s.TotalAssignedCount,
                Trust = s.Trust,
                UpdatedAtUtc = s.UpdatedAtUtc,
                MissRate = missRate,
                TrustLevel = GetTrustLevel(s.Trust)
            };
        }
        private async Task<List<ScoreLogDto>> GetLogs(int memberId, int take, CancellationToken ct)
        {
            take = Math.Clamp(take, 1, 200);

            return await _context.MemberScoreLogs
                .AsNoTracking()
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .Select(x => new ScoreLogDto
                {
                    Delta = x.Delta,
                    IsIncrease = x.IsIncrease,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(ct);
        }

        private async Task<List<ScorePriorityStatDto>> GetPriorityStats(int memberId, CancellationToken ct)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var priorities = await _context.TaskPriorities
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new
                {
                    p.Id,
                    Title = string.IsNullOrWhiteSpace(p.Title) ? p.Code : p.Title
                })
                .ToListAsync(ct);

            var rows = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t =>
                    t.AssignedToMemberId == memberId &&
                    t.Project.OrganizationId == orgId &&
                    !t.IsArchived &&
                    (t.IsDone || t.IsMissed))
                .GroupBy(t => t.PriorityId)
                .Select(g => new
                {
                    PriorityId = g.Key,
                    CompletedCount = g.Count(t => t.IsDone),
                    MissedCount = g.Count(t => t.IsMissed),
                    TotalCount = g.Count()
                })
                .ToListAsync(ct);

            var statsByPriority = rows.ToDictionary(x => x.PriorityId);

            return priorities
                .Select(priority =>
                {
                    statsByPriority.TryGetValue(priority.Id, out var stat);

                    return new ScorePriorityStatDto
                    {
                        PriorityId = priority.Id,
                        PriorityTitle = priority.Title,
                        CompletedCount = stat?.CompletedCount ?? 0,
                        MissedCount = stat?.MissedCount ?? 0,
                        TotalCount = stat?.TotalCount ?? 0
                    };
                })
                .ToList();
        }


        private static string GetTrustLevel(decimal trust) => trust switch
        {
            >= 80 => "Надёжный",
            >= 50 => "Под наблюдением",
            _ => "Риск"
        };
    }
}
