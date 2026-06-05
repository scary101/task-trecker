using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class ProjectTask
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ProjectId { get; set; }

    public int? TeamId { get; set; }

    public int CreatedByMemberId { get; set; }

    public int? AssignedToMemberId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateTime? Deadline { get; set; }

    public bool IsArchived { get; set; }

    public bool IsDone { get; set; }

    public bool IsMissed { get; set; }

    public int PriorityId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Member? AssignedToMember { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual Member CreatedByMember { get; set; } = null!;

    public virtual TaskPriority PriorityNavigation { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectCalendarEvent> ProjectCalendarEvents { get; set; } = new List<ProjectCalendarEvent>();

    public virtual ICollection<ProjectTaskChecklistItem> ProjectTaskChecklistItems { get; set; } = new List<ProjectTaskChecklistItem>();

    public virtual ProjectTeam? Team { get; set; }
}
