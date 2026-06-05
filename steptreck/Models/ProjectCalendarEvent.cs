using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class ProjectCalendarEvent
{
    public int Id { get; set; }

    public int CreatedByMemberId { get; set; }

    public int? TaskId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public string? Description { get; set; }

    public bool IsPinned { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int TeamId { get; set; }

    public short TypeId { get; set; }

    public virtual Member CreatedByMember { get; set; } = null!;

    public virtual ProjectTask? Task { get; set; }

    public virtual ProjectTeam Team { get; set; } = null!;
}
