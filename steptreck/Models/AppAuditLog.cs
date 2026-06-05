using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class AppAuditLog
{
    public long Id { get; set; }

    public int OrganizationId { get; set; }

    public int? TeamId { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public int? ActorMemberId { get; set; }

    public int? TargetMemberId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public long? EntityId { get; set; }

    public string? EntityName { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }
}
