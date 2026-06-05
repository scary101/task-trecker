namespace steptreck.Domain.DTOs.DashboardDTOs;

public sealed class ProjectDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectDescription { get; set; }
    public ProjectDashboardHealthDto Health { get; set; } = new();
    public ProjectDashboardTaskSummaryDto Tasks { get; set; } = new();
    public ProjectDashboardWorkSummaryDto Workload { get; set; } = new();
    public List<ProjectDashboardTeamDto> Teams { get; set; } = new();
    public List<ProjectDashboardMemberDto> MemberRisks { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public sealed class TeamDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int ProjectId { get; set; }
    public int TeamId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string? TeamDescription { get; set; }
    public string? LeadFullName { get; set; }
    public TeamDashboardHealthDto Health { get; set; } = new();
    public ProjectDashboardTaskSummaryDto Tasks { get; set; } = new();
    public ProjectDashboardWorkSummaryDto Workload { get; set; } = new();
    public List<TeamDashboardMemberDto> Members { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public sealed class ProjectDashboardHealthDto
{
    public decimal DeliveryScore { get; set; }
    public decimal DeadlinePressure { get; set; }
    public decimal WorkloadBalance { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal RiskMemberRate { get; set; }
}

public sealed class TeamDashboardHealthDto
{
    public decimal TeamPulse { get; set; }
    public decimal BusFactorRisk { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal AverageTrust { get; set; }
    public decimal WorkloadBalance { get; set; }
}

public class ProjectDashboardTaskSummaryDto
{
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueTodayTasks { get; set; }
    public int DueThisWeekTasks { get; set; }
    public int UnassignedTasks { get; set; }
    public int MissedTasks { get; set; }
}

public sealed class TeamDashboardTaskSummaryDto : ProjectDashboardTaskSummaryDto
{
}

public class ProjectDashboardWorkSummaryDto
{
    public int ActiveSessions { get; set; }
    public int WorkingSessions { get; set; }
    public int PausedSessions { get; set; }
    public long WorkedSecondsToday { get; set; }
    public long WorkedSecondsLast7Days { get; set; }
}

public sealed class TeamDashboardWorkSummaryDto : ProjectDashboardWorkSummaryDto
{
}

public sealed class ProjectDashboardTeamDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int MembersCount { get; set; }
    public int ActiveTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int UnassignedTasks { get; set; }
    public int ActiveSessions { get; set; }
    public decimal LoadShare { get; set; }
}

public class ProjectDashboardMemberDto
{
    public int MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public decimal Trust { get; set; }
    public decimal MissRate { get; set; }
    public int ActiveTasks { get; set; }
    public int OverdueTasks { get; set; }
}

public sealed class TeamDashboardMemberDto : ProjectDashboardMemberDto
{
    public int CompletedCount { get; set; }
    public int MissedCount { get; set; }
    public bool HasActiveSession { get; set; }
}
