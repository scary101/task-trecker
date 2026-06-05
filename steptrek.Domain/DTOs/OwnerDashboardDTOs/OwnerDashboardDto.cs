namespace steptreck.Domain.DTOs.OwnerDashboardDTOs;

public sealed class OwnerDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public OwnerOrganizationSummaryDto Organization { get; set; } = new();

    public OwnerSubscriptionSummaryDto Subscription { get; set; } = new();

    public OwnerUsageSummaryDto Usage { get; set; } = new();

    public OwnerTaskRiskSummaryDto TaskRisks { get; set; } = new();

    public OwnerWorkloadSummaryDto Workload { get; set; } = new();

    public List<OwnerRoleCountDto> Roles { get; set; } = new();

    public List<OwnerProjectSummaryDto> Projects { get; set; } = new();

    public List<OwnerTeamSummaryDto> Teams { get; set; } = new();

    public List<OwnerMemberRiskDto> MemberRisks { get; set; } = new();

    public List<OwnerPaymentSummaryDto> RecentPayments { get; set; } = new();
}

public sealed class OwnerOrganizationSummaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class OwnerSubscriptionSummaryDto
{
    public bool HasActive { get; set; }

    public int? SubscriptionId { get; set; }

    public int? PlanId { get; set; }

    public string? PlanName { get; set; }

    public string? StatusCode { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public int? DaysLeft { get; set; }

    public string? Currency { get; set; }

    public int PriceCentsPerMonth { get; set; }

    public bool AllowInvites { get; set; }

    public bool AllowNewProjects { get; set; }

    public bool AllowNewTeams { get; set; }
}

public sealed class OwnerUsageSummaryDto
{
    public int MembersCount { get; set; }

    public int? MaxMembers { get; set; }

    public int? MembersLeft { get; set; }

    public int ProjectsCount { get; set; }

    public int? MaxProjects { get; set; }

    public int? ProjectsLeft { get; set; }

    public int TeamsCount { get; set; }

    public int? MaxTeams { get; set; }

    public int? TeamsLeft { get; set; }

    public bool CanInviteMember { get; set; }

    public bool CanCreateProject { get; set; }

    public bool CanCreateTeam { get; set; }
}

public sealed class OwnerRoleCountDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class OwnerTaskRiskSummaryDto
{
    public int TotalActiveTasks { get; set; }

    public int DoneTasks { get; set; }

    public int OverdueTasks { get; set; }

    public int DueTodayTasks { get; set; }

    public int DueThisWeekTasks { get; set; }

    public int UnassignedTasks { get; set; }

    public int MissedTasks { get; set; }
}

public sealed class OwnerWorkloadSummaryDto
{
    public int ActiveSessions { get; set; }

    public int WorkingSessions { get; set; }

    public int PausedSessions { get; set; }

    public int CompletedSessionsToday { get; set; }

    public long WorkedSecondsToday { get; set; }

    public long WorkedSecondsLast7Days { get; set; }
}

public sealed class OwnerProjectSummaryDto
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int TeamsCount { get; set; }

    public int MembersCount { get; set; }

    public int ActiveTasks { get; set; }

    public int OverdueTasks { get; set; }

    public int DoneTasks { get; set; }
}

public sealed class OwnerTeamSummaryDto
{
    public int TeamId { get; set; }

    public int ProjectId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public int MembersCount { get; set; }

    public int ActiveTasks { get; set; }

    public int OverdueTasks { get; set; }

    public int ActiveSessions { get; set; }
}

public sealed class OwnerMemberRiskDto
{
    public int MemberId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public decimal Trust { get; set; }

    public int CompletedCount { get; set; }

    public int MissedCount { get; set; }

    public decimal MissRate { get; set; }

    public int ActiveTasks { get; set; }

    public int OverdueTasks { get; set; }
}

public sealed class OwnerPaymentSummaryDto
{
    public long Id { get; set; }

    public int AmountCents { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public bool HasReceipt { get; set; }
}
