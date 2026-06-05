using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.DashboardDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Dashboards;

public sealed class ProjectTeamDashboardService
{
    private readonly AppDbContext _context;
    private readonly UserHelper _userHelper;

    public ProjectTeamDashboardService(AppDbContext context, UserHelper userHelper)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task<ProjectDashboardDto> GetProjectDashboardAsync(int projectId, CancellationToken ct = default)
    {
        var orgId = _userHelper.GetCurrentOrganizationId();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-6);
        var weekEnd = todayStart.AddDays(7);

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId && p.OrganizationId == orgId && !p.IsArchived)
            .Select(p => new { p.Id, p.Name, p.Description })
            .FirstOrDefaultAsync(ct);

        if (project is null)
            throw new InvalidOperationException("Проект не найден или недоступен.");

        var tasks = await GetTaskSummaryAsync(projectId, teamId: null, now, todayStart, tomorrowStart, weekEnd, ct);
        var workload = await GetWorkSummaryAsync(projectId, teamId: null, todayStart, weekStart, ct);
        var teams = await GetProjectTeamsAsync(projectId, orgId, tasks.ActiveTasks, ct);
        var members = await GetProjectMemberRisksAsync(projectId, orgId, now, ct);

        var riskMemberRate = Percent(members.Count(x => x.Trust < 50 || x.OverdueTasks > 0), Math.Max(1, members.Count));
        var onTimeRate = Percent(tasks.DoneTasks, Math.Max(1, tasks.DoneTasks + tasks.MissedTasks));
        var unassignedRate = Percent(tasks.UnassignedTasks, Math.Max(1, tasks.ActiveTasks));
        var overdueRate = Percent(tasks.OverdueTasks, Math.Max(1, tasks.ActiveTasks));
        var balance = CalculateBalance(teams.Select(x => x.ActiveTasks));

        var dto = new ProjectDashboardDto
        {
            GeneratedAtUtc = now,
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectDescription = project.Description,
            Tasks = tasks,
            Workload = workload,
            Teams = teams,
            MemberRisks = members
                .OrderByDescending(x => x.OverdueTasks)
                .ThenBy(x => x.Trust)
                .ThenByDescending(x => x.MissRate)
                .Take(8)
                .ToList(),
            Health = new ProjectDashboardHealthDto
            {
                OnTimeRate = onTimeRate,
                RiskMemberRate = riskMemberRate,
                WorkloadBalance = balance,
                DeadlinePressure = Math.Round(tasks.OverdueTasks * 2m + tasks.DueTodayTasks * 1.2m + tasks.DueThisWeekTasks * 0.4m + tasks.UnassignedTasks * 0.8m, 2),
                DeliveryScore = ClampScore(100 - overdueRate * 0.55m - unassignedRate * 0.2m - riskMemberRate * 0.25m)
            }
        };

        dto.Insights = BuildProjectInsights(dto);
        return dto;
    }

    public async Task<TeamDashboardDto> GetTeamDashboardAsync(int projectId, int teamId, CancellationToken ct = default)
    {
        var orgId = _userHelper.GetCurrentOrganizationId();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-6);
        var weekEnd = todayStart.AddDays(7);

        var team = await _context.ProjectTeams
            .AsNoTracking()
            .Where(t => t.Id == teamId && t.ProjectId == projectId && t.Project.OrganizationId == orgId && t.IsActive && !t.Project.IsArchived)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.ProjectId,
                ProjectName = t.Project.Name,
                Lead = t.ProjectTeamMembers
                    .Where(ptm => ptm.TeamRole == "Руководитель команды")
                    .Select(ptm => (ptm.Member.Surname + " " + ptm.Member.Name + " " + (ptm.Member.Patronymic ?? "")).Trim())
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (team is null)
            throw new InvalidOperationException("Команда не найдена или недоступна.");

        var tasks = await GetTaskSummaryAsync(projectId, teamId, now, todayStart, tomorrowStart, weekEnd, ct);
        var workload = await GetWorkSummaryAsync(projectId, teamId, todayStart, weekStart, ct);
        var members = await GetTeamMembersAsync(teamId, orgId, now, ct);

        var onTimeRate = Percent(tasks.DoneTasks, Math.Max(1, tasks.DoneTasks + tasks.MissedTasks));
        var avgTrust = members.Count == 0 ? 100 : Math.Round(members.Average(x => x.Trust), 2);
        var balance = CalculateBalance(members.Select(x => x.ActiveTasks));
        var maxLoadShare = members.Count == 0 ? 0 : Percent(members.Max(x => x.ActiveTasks), Math.Max(1, tasks.ActiveTasks));
        var activityRate = Percent(members.Count(x => x.HasActiveSession), Math.Max(1, members.Count));
        var overdueRate = Percent(tasks.OverdueTasks, Math.Max(1, tasks.ActiveTasks));

        var dto = new TeamDashboardDto
        {
            GeneratedAtUtc = now,
            ProjectId = projectId,
            TeamId = team.Id,
            ProjectName = team.ProjectName,
            TeamName = team.Name,
            TeamDescription = team.Description,
            LeadFullName = team.Lead,
            Tasks = tasks,
            Workload = workload,
            Members = members
                .OrderByDescending(x => x.OverdueTasks)
                .ThenByDescending(x => x.ActiveTasks)
                .ThenBy(x => x.Trust)
                .ToList(),
            Health = new TeamDashboardHealthDto
            {
                OnTimeRate = onTimeRate,
                AverageTrust = avgTrust,
                WorkloadBalance = balance,
                BusFactorRisk = maxLoadShare,
                TeamPulse = ClampScore(onTimeRate * 0.35m + avgTrust * 0.35m + balance * 0.2m + activityRate * 0.1m - overdueRate * 0.25m)
            }
        };

        dto.Insights = BuildTeamInsights(dto);
        return dto;
    }

    private async Task<ProjectDashboardTaskSummaryDto> GetTaskSummaryAsync(
        int projectId,
        int? teamId,
        DateTime now,
        DateTime todayStart,
        DateTime tomorrowStart,
        DateTime weekEnd,
        CancellationToken ct)
    {
        var query = _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && !t.IsArchived && !t.Project.IsArchived && (t.TeamId == null || t.Team!.IsActive));

        if (teamId.HasValue)
            query = query.Where(t => t.TeamId == teamId.Value);

        return new ProjectDashboardTaskSummaryDto
        {
            TotalTasks = await query.CountAsync(ct),
            ActiveTasks = await query.CountAsync(t => !t.IsDone && t.Status != "Done", ct),
            DoneTasks = await query.CountAsync(t => t.IsDone || t.Status == "Done", ct),
            OverdueTasks = await query.CountAsync(t => !t.IsDone && t.Status != "Done" && t.Deadline != null && t.Deadline < now, ct),
            DueTodayTasks = await query.CountAsync(t => !t.IsDone && t.Status != "Done" && t.Deadline >= todayStart && t.Deadline < tomorrowStart, ct),
            DueThisWeekTasks = await query.CountAsync(t => !t.IsDone && t.Status != "Done" && t.Deadline >= todayStart && t.Deadline < weekEnd, ct),
            UnassignedTasks = await query.CountAsync(t => !t.IsDone && t.Status != "Done" && t.AssignedToMemberId == null, ct),
            MissedTasks = await query.CountAsync(t => t.IsMissed, ct)
        };
    }

    private async Task<ProjectDashboardWorkSummaryDto> GetWorkSummaryAsync(
        int projectId,
        int? teamId,
        DateTime todayStart,
        DateTime weekStart,
        CancellationToken ct)
    {
        var sessions = _context.WorkSessions
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId);

        if (teamId.HasValue)
            sessions = sessions.Where(s => s.TeamId == teamId.Value);

        var active = sessions.Where(s => s.Status == (int)WorkStatus.Working || s.Status == (int)WorkStatus.Paused);
        var today = sessions.Where(s => s.Status == (int)WorkStatus.Completed && s.EndedAtUtc != null && s.EndedAtUtc >= todayStart);
        var week = sessions.Where(s => s.Status == (int)WorkStatus.Completed && s.EndedAtUtc != null && s.EndedAtUtc >= weekStart);

        return new ProjectDashboardWorkSummaryDto
        {
            ActiveSessions = await active.CountAsync(ct),
            WorkingSessions = await active.CountAsync(s => s.Status == (int)WorkStatus.Working, ct),
            PausedSessions = await active.CountAsync(s => s.Status == (int)WorkStatus.Paused, ct),
            WorkedSecondsToday = await today.SumAsync(s => (long)s.DurationSeconds, ct),
            WorkedSecondsLast7Days = await week.SumAsync(s => (long)s.DurationSeconds, ct)
        };
    }

    private async Task<List<ProjectDashboardTeamDto>> GetProjectTeamsAsync(int projectId, int orgId, int projectActiveTasks, CancellationToken ct)
    {
        var rows = await _context.ProjectTeams
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.Project.OrganizationId == orgId && t.IsActive && !t.Project.IsArchived)
            .Select(t => new ProjectDashboardTeamDto
            {
                TeamId = t.Id,
                TeamName = t.Name,
                MembersCount = t.ProjectTeamMembers.Count(ptm => ptm.Member.IsActive),
                ActiveTasks = t.ProjectTasks.Count(task => !task.IsArchived && !task.IsDone && task.Status != "Done"),
                OverdueTasks = t.ProjectTasks.Count(task => !task.IsArchived && !task.IsDone && task.Status != "Done" && task.Deadline != null && task.Deadline < DateTime.UtcNow),
                UnassignedTasks = t.ProjectTasks.Count(task => !task.IsArchived && !task.IsDone && task.Status != "Done" && task.AssignedToMemberId == null),
                ActiveSessions = t.WorkSessions.Count(s => s.Status == (int)WorkStatus.Working || s.Status == (int)WorkStatus.Paused)
            })
            .ToListAsync(ct);

        foreach (var row in rows)
            row.LoadShare = Percent(row.ActiveTasks, Math.Max(1, projectActiveTasks));

        return rows
            .OrderByDescending(x => x.OverdueTasks)
            .ThenByDescending(x => x.ActiveTasks)
            .ThenBy(x => x.TeamName)
            .ToList();
    }

    private async Task<List<ProjectDashboardMemberDto>> GetProjectMemberRisksAsync(int projectId, int orgId, DateTime now, CancellationToken ct)
    {
        return await _context.ProjectTeamMembers
            .AsNoTracking()
            .Where(ptm => ptm.Team.ProjectId == projectId && ptm.Team.Project.OrganizationId == orgId && ptm.Team.IsActive && ptm.Member.IsActive)
            .Select(ptm => new ProjectDashboardMemberDto
            {
                MemberId = ptm.MemberId,
                FullName = (ptm.Member.Surname + " " + ptm.Member.Name + " " + (ptm.Member.Patronymic ?? "")).Trim(),
                TeamName = ptm.Team.Name,
                Trust = ptm.Member.MemberScore != null ? ptm.Member.MemberScore.Trust : 100,
                MissRate = ptm.Member.MemberScore != null && (ptm.Member.MemberScore.CompletedCount + ptm.Member.MemberScore.MissedCount) > 0
                    ? Math.Round((decimal)ptm.Member.MemberScore.MissedCount / (ptm.Member.MemberScore.CompletedCount + ptm.Member.MemberScore.MissedCount) * 100, 2)
                    : 0,
                ActiveTasks = ptm.Member.ProjectTaskAssignedToMembers.Count(t => t.ProjectId == projectId && !t.IsArchived && !t.IsDone && t.Status != "Done"),
                OverdueTasks = ptm.Member.ProjectTaskAssignedToMembers.Count(t => t.ProjectId == projectId && !t.IsArchived && !t.IsDone && t.Status != "Done" && t.Deadline != null && t.Deadline < now)
            })
            .ToListAsync(ct);
    }

    private async Task<List<TeamDashboardMemberDto>> GetTeamMembersAsync(int teamId, int orgId, DateTime now, CancellationToken ct)
    {
        return await _context.ProjectTeamMembers
            .AsNoTracking()
            .Where(ptm => ptm.TeamId == teamId && ptm.Team.Project.OrganizationId == orgId && ptm.Team.IsActive && ptm.Member.IsActive)
            .Select(ptm => new TeamDashboardMemberDto
            {
                MemberId = ptm.MemberId,
                FullName = (ptm.Member.Surname + " " + ptm.Member.Name + " " + (ptm.Member.Patronymic ?? "")).Trim(),
                TeamName = ptm.Team.Name,
                Trust = ptm.Member.MemberScore != null ? ptm.Member.MemberScore.Trust : 100,
                CompletedCount = ptm.Member.MemberScore != null ? ptm.Member.MemberScore.CompletedCount : 0,
                MissedCount = ptm.Member.MemberScore != null ? ptm.Member.MemberScore.MissedCount : 0,
                MissRate = ptm.Member.MemberScore != null && (ptm.Member.MemberScore.CompletedCount + ptm.Member.MemberScore.MissedCount) > 0
                    ? Math.Round((decimal)ptm.Member.MemberScore.MissedCount / (ptm.Member.MemberScore.CompletedCount + ptm.Member.MemberScore.MissedCount) * 100, 2)
                    : 0,
                ActiveTasks = ptm.Member.ProjectTaskAssignedToMembers.Count(t => t.TeamId == teamId && !t.IsArchived && !t.IsDone && t.Status != "Done"),
                OverdueTasks = ptm.Member.ProjectTaskAssignedToMembers.Count(t => t.TeamId == teamId && !t.IsArchived && !t.IsDone && t.Status != "Done" && t.Deadline != null && t.Deadline < now),
                HasActiveSession = ptm.Member.UserId != null && ptm.Member.User!.WorkSessions.Any(s => s.TeamId == teamId && (s.Status == (int)WorkStatus.Working || s.Status == (int)WorkStatus.Paused))
            })
            .ToListAsync(ct);
    }

    private static List<string> BuildProjectInsights(ProjectDashboardDto dto)
    {
        var insights = new List<string>();
        if (dto.Tasks.OverdueTasks > 0)
            insights.Add($"Просрочено {dto.Tasks.OverdueTasks} задач: это главный фактор давления на проект.");
        if (dto.Tasks.UnassignedTasks > 0)
            insights.Add($"{dto.Tasks.UnassignedTasks} активных задач без ответственного. Их стоит назначить первыми.");
        if (dto.Health.WorkloadBalance < 60)
            insights.Add("Нагрузка между командами распределена неровно: проверьте команды с максимальной долей задач.");
        if (dto.MemberRisks.Any(x => x.Trust < 50))
            insights.Add("Есть участники с trust ниже 50: лучше не класть на них критичные дедлайны без поддержки.");
        if (insights.Count == 0)
            insights.Add("Проект выглядит стабильно: критичных перекосов по дедлайнам и нагрузке не видно.");
        return insights;
    }

    private static List<string> BuildTeamInsights(TeamDashboardDto dto)
    {
        var insights = new List<string>();
        if (dto.Health.BusFactorRisk >= 50)
            insights.Add("Слишком много активных задач сосредоточено на одном участнике: высокий bus factor risk.");
        if (dto.Tasks.OverdueTasks > 0)
            insights.Add($"У команды {dto.Tasks.OverdueTasks} просроченных задач. Это лучше разобрать до новых назначений.");
        if (dto.Health.TeamPulse < 60)
            insights.Add("Team Pulse ниже комфортного уровня: команда проседает по срокам, trust или балансу нагрузки.");
        if (dto.Tasks.UnassignedTasks > 0)
            insights.Add($"{dto.Tasks.UnassignedTasks} задач команды пока без ответственного.");
        if (insights.Count == 0)
            insights.Add("Команда в хорошем состоянии: нагрузка и сроки выглядят управляемо.");
        return insights;
    }

    private static decimal Percent(int value, int total) =>
        total <= 0 ? 0 : Math.Round((decimal)value / total * 100, 2);

    private static decimal CalculateBalance(IEnumerable<int> loads)
    {
        var values = loads.ToList();
        if (values.Count <= 1 || values.Sum() == 0)
            return 100;

        var avg = values.Average();
        var variance = values.Average(x => Math.Pow(x - avg, 2));
        var cv = avg == 0 ? 0 : Math.Sqrt(variance) / avg;
        return ClampScore(100 - (decimal)cv * 45);
    }

    private static decimal ClampScore(decimal value) =>
        Math.Round(Math.Clamp(value, 0, 100), 2);
}
