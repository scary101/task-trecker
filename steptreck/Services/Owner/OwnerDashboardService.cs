using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.OwnerDashboardDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Owner;

public class OwnerDashboardService
{
    private readonly AppDbContext _context;
    private readonly UserHelper _userHelper;

    public OwnerDashboardService(AppDbContext context, UserHelper userHelper)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var orgId = _userHelper.GetCurrentOrganizationId();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-6);
        var weekEnd = todayStart.AddDays(7);

        var organization = await GetOrganizationAsync(orgId, ct);
        var subscription = await GetSubscriptionAsync(orgId, now, ct);
        var usage = await GetUsageAsync(orgId, subscription, ct);

        return new OwnerDashboardDto
        {
            GeneratedAtUtc = now,
            Organization = organization,
            Subscription = subscription,
            Usage = usage,
            Roles = await GetRoleCountsAsync(orgId, ct),
            TaskRisks = await GetTaskRisksAsync(orgId, now, todayStart, tomorrowStart, weekEnd, ct),
            Workload = await GetWorkloadAsync(orgId, todayStart, weekStart, ct),
            Projects = await GetProjectsAsync(orgId, now, ct),
            Teams = await GetTeamsAsync(orgId, now, ct),
            MemberRisks = await GetMemberRisksAsync(orgId, now, ct),
            RecentPayments = await GetRecentPaymentsAsync(orgId, ct)
        };
    }

    private async Task<OwnerOrganizationSummaryDto> GetOrganizationAsync(int orgId, CancellationToken ct)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .Where(o => o.Id == orgId)
            .Select(o => new OwnerOrganizationSummaryDto
            {
                Id = o.Id,
                Name = o.Name,
                CreatedAtUtc = o.CreatedAt,
                UpdatedAtUtc = o.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        return organization ?? throw new InvalidOperationException("Организация не найдена.");
    }

    private async Task<OwnerSubscriptionSummaryDto> GetSubscriptionAsync(int orgId, DateTime now, CancellationToken ct)
    {
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.Status)
            .Where(s => s.OrganizationId == orgId && s.EndDate != null && s.EndDate > now)
            .OrderByDescending(s => s.EndDate)
            .Select(s => new OwnerSubscriptionSummaryDto
            {
                HasActive = s.Status.Code == "active",
                SubscriptionId = s.Id,
                PlanId = s.PlanId,
                PlanName = s.Plan != null ? s.Plan.Name : null,
                StatusCode = s.Status.Code,
                StartDateUtc = s.StartDate,
                EndDateUtc = s.EndDate,
                DaysLeft = s.EndDate != null ? (int)Math.Floor((s.EndDate.Value - now).TotalDays) : null,
                Currency = s.Currency,
                PriceCentsPerMonth = s.PriceCents,
                AllowInvites = s.AllowInvites,
                AllowNewProjects = s.AllowNewProjects,
                AllowNewTeams = s.AllowNewTeams
            })
            .FirstOrDefaultAsync(ct);

        return subscription ?? new OwnerSubscriptionSummaryDto();
    }

    private async Task<OwnerUsageSummaryDto> GetUsageAsync(
        int orgId,
        OwnerSubscriptionSummaryDto subscription,
        CancellationToken ct)
    {
        var membersCount = await _context.Members
            .AsNoTracking()
            .CountAsync(m => m.OrganizationId == orgId && m.IsActive, ct);

        var projectsCount = await _context.Projects
            .AsNoTracking()
            .CountAsync(p => p.OrganizationId == orgId && !p.IsArchived, ct);

        var teamsCount = await _context.ProjectTeams
            .AsNoTracking()
            .CountAsync(t => t.Project.OrganizationId == orgId && t.IsActive && !t.Project.IsArchived, ct);

        int? maxMembers = null;
        int? maxProjects = null;
        int? maxTeams = null;

        if (subscription.SubscriptionId.HasValue)
        {
            var limits = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.Id == subscription.SubscriptionId.Value)
                .Select(s => new { s.MaxMembers, s.MaxProjects, s.MaxTeams })
                .FirstAsync(ct);

            maxMembers = LimitOrNull(limits.MaxMembers);
            maxProjects = LimitOrNull(limits.MaxProjects);
            maxTeams = LimitOrNull(limits.MaxTeams);
        }

        return new OwnerUsageSummaryDto
        {
            MembersCount = membersCount,
            MaxMembers = maxMembers,
            MembersLeft = Left(maxMembers, membersCount),
            ProjectsCount = projectsCount,
            MaxProjects = maxProjects,
            ProjectsLeft = Left(maxProjects, projectsCount),
            TeamsCount = teamsCount,
            MaxTeams = maxTeams,
            TeamsLeft = Left(maxTeams, teamsCount),
            CanInviteMember = subscription.HasActive && subscription.AllowInvites && CanAdd(maxMembers, membersCount),
            CanCreateProject = subscription.HasActive && subscription.AllowNewProjects && CanAdd(maxProjects, projectsCount),
            CanCreateTeam = subscription.HasActive && subscription.AllowNewTeams && CanAdd(maxTeams, teamsCount)
        };
    }

    private async Task<List<OwnerRoleCountDto>> GetRoleCountsAsync(int orgId, CancellationToken ct)
    {
        return await _context.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId && m.IsActive)
            .GroupBy(m => new { m.RoleId, m.Role.Name })
            .Select(g => new OwnerRoleCountDto
            {
                RoleId = g.Key.RoleId,
                RoleName = g.Key.Name,
                Count = g.Count()
            })
            .OrderBy(x => x.RoleId)
            .ToListAsync(ct);
    }

    private async Task<OwnerTaskRiskSummaryDto> GetTaskRisksAsync(
        int orgId,
        DateTime now,
        DateTime todayStart,
        DateTime tomorrowStart,
        DateTime weekEnd,
        CancellationToken ct)
    {
        var query = _context.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                t.Project.OrganizationId == orgId &&
                !t.Project.IsArchived &&
                (t.TeamId == null || t.Team!.IsActive) &&
                !t.IsArchived);

        return new OwnerTaskRiskSummaryDto
        {
            TotalActiveTasks = await query.CountAsync(ct),
            DoneTasks = await query.CountAsync(t => t.IsDone || t.Status == "Done", ct),
            OverdueTasks = await query.CountAsync(t => !t.IsDone && t.Deadline != null && t.Deadline < now, ct),
            DueTodayTasks = await query.CountAsync(t => !t.IsDone && t.Deadline >= todayStart && t.Deadline < tomorrowStart, ct),
            DueThisWeekTasks = await query.CountAsync(t => !t.IsDone && t.Deadline >= todayStart && t.Deadline < weekEnd, ct),
            UnassignedTasks = await query.CountAsync(t => t.AssignedToMemberId == null, ct),
            MissedTasks = await query.CountAsync(t => t.IsMissed, ct)
        };
    }

    private async Task<OwnerWorkloadSummaryDto> GetWorkloadAsync(
        int orgId,
        DateTime todayStart,
        DateTime weekStart,
        CancellationToken ct)
    {
        var activeStatuses = _context.EmployeeWorkStatuses
            .AsNoTracking()
            .Where(s =>
                s.OrgId == orgId &&
                s.CurrentSessionId != null &&
                (s.CurrentStatus == (int)WorkStatus.Working ||
                 s.CurrentStatus == (int)WorkStatus.Paused));

        var completedToday = _context.WorkSessions
            .AsNoTracking()
            .Where(s =>
                s.OrgId == orgId &&
                s.Status == (int)WorkStatus.Completed &&
                s.EndedAtUtc != null &&
                s.EndedAtUtc >= todayStart);

        var completedLast7Days = _context.WorkSessions
            .AsNoTracking()
            .Where(s =>
                s.OrgId == orgId &&
                s.Status == (int)WorkStatus.Completed &&
                s.EndedAtUtc != null &&
                s.EndedAtUtc >= weekStart);

        return new OwnerWorkloadSummaryDto
        {
            ActiveSessions = await activeStatuses.CountAsync(ct),
            WorkingSessions = await activeStatuses.CountAsync(s => s.CurrentStatus == (int)WorkStatus.Working, ct),
            PausedSessions = await activeStatuses.CountAsync(s => s.CurrentStatus == (int)WorkStatus.Paused, ct),
            CompletedSessionsToday = await completedToday.CountAsync(ct),
            WorkedSecondsToday = await completedToday.SumAsync(s => (long)s.DurationSeconds, ct),
            WorkedSecondsLast7Days = await completedLast7Days.SumAsync(s => (long)s.DurationSeconds, ct)
        };
    }

    private async Task<List<OwnerProjectSummaryDto>> GetProjectsAsync(int orgId, DateTime now, CancellationToken ct)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId && !p.IsArchived)
            .Select(p => new OwnerProjectSummaryDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                TeamsCount = p.ProjectTeams.Count(t => t.IsActive),
                MembersCount = p.ProjectTeams
                    .Where(t => t.IsActive)
                    .SelectMany(t => t.ProjectTeamMembers)
                    .Select(ptm => ptm.MemberId)
                    .Distinct()
                    .Count(),
                ActiveTasks = p.ProjectTasks.Count(t => !t.IsArchived && !t.IsDone),
                OverdueTasks = p.ProjectTasks.Count(t => !t.IsArchived && !t.IsDone && t.Deadline != null && t.Deadline < now),
                DoneTasks = p.ProjectTasks.Count(t => !t.IsArchived && (t.IsDone || t.Status == "Done"))
            })
            .OrderByDescending(p => p.OverdueTasks)
            .ThenByDescending(p => p.ActiveTasks)
            .ThenBy(p => p.ProjectName)
            .ToListAsync(ct);
    }

    private async Task<List<OwnerTeamSummaryDto>> GetTeamsAsync(int orgId, DateTime now, CancellationToken ct)
    {
        return await _context.ProjectTeams
            .AsNoTracking()
            .Where(t => t.Project.OrganizationId == orgId && t.IsActive && !t.Project.IsArchived)
            .Select(t => new OwnerTeamSummaryDto
            {
                TeamId = t.Id,
                ProjectId = t.ProjectId,
                TeamName = t.Name,
                ProjectName = t.Project.Name,
                MembersCount = t.ProjectTeamMembers.Count(ptm => ptm.Member.IsActive),
                ActiveTasks = t.ProjectTasks.Count(task => !task.IsArchived && !task.IsDone),
                OverdueTasks = t.ProjectTasks.Count(task => !task.IsArchived && !task.IsDone && task.Deadline != null && task.Deadline < now),
                ActiveSessions = t.WorkSessions.Count(s =>
                    s.Status == (int)WorkStatus.Working ||
                    s.Status == (int)WorkStatus.Paused)
            })
            .OrderByDescending(t => t.OverdueTasks)
            .ThenByDescending(t => t.ActiveTasks)
            .ThenBy(t => t.TeamName)
            .Take(20)
            .ToListAsync(ct);
    }

    private async Task<List<OwnerMemberRiskDto>> GetMemberRisksAsync(int orgId, DateTime now, CancellationToken ct)
    {
        return await _context.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId && m.IsActive)
            .Select(m => new OwnerMemberRiskDto
            {
                MemberId = m.Id,
                FullName = (m.Surname + " " + m.Name + " " + (m.Patronymic ?? "")).Trim(),
                RoleName = m.Role.Name,
                Trust = m.MemberScore != null ? m.MemberScore.Trust : 100,
                CompletedCount = m.MemberScore != null ? m.MemberScore.CompletedCount : 0,
                MissedCount = m.MemberScore != null ? m.MemberScore.MissedCount : 0,
                MissRate = m.MemberScore != null && (m.MemberScore.CompletedCount + m.MemberScore.MissedCount) > 0
                    ? Math.Round((decimal)m.MemberScore.MissedCount / (m.MemberScore.CompletedCount + m.MemberScore.MissedCount) * 100, 2)
                    : 0,
                ActiveTasks = m.ProjectTaskAssignedToMembers.Count(t =>
                    !t.IsArchived &&
                    !t.IsDone &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive)),
                OverdueTasks = m.ProjectTaskAssignedToMembers.Count(t =>
                    !t.IsArchived &&
                    !t.IsDone &&
                    t.Deadline != null &&
                    t.Deadline < now &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive))
            })
            .OrderBy(x => x.Trust)
            .ThenByDescending(x => x.OverdueTasks)
            .ThenByDescending(x => x.MissRate)
            .Take(10)
            .ToListAsync(ct);
    }

    private async Task<List<OwnerPaymentSummaryDto>> GetRecentPaymentsAsync(int orgId, CancellationToken ct)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new OwnerPaymentSummaryDto
            {
                Id = p.Id,
                AmountCents = p.AmountCents,
                Currency = p.Currency,
                Status = p.Status,
                Reason = p.Reason,
                CreatedAtUtc = p.CreatedAt,
                PaidAtUtc = p.PaidAtUtc,
                HasReceipt = p.ReceiptObjectKey != null
            })
            .ToListAsync(ct);
    }

    private static int? LimitOrNull(int limit) => limit > 0 ? limit : null;

    private static int? Left(int? limit, int current)
    {
        if (!limit.HasValue)
            return null;

        return Math.Max(0, limit.Value - current);
    }

    private static bool CanAdd(int? limit, int current) =>
        !limit.HasValue || current + 1 <= limit.Value;
}
