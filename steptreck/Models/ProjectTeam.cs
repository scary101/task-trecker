using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class ProjectTeam
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public string? CardBackgroundUrl { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectCalendarEvent> ProjectCalendarEvents { get; set; } = new List<ProjectCalendarEvent>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTeamMember> ProjectTeamMembers { get; set; } = new List<ProjectTeamMember>();

    public virtual ICollection<TeamChatMessage> TeamChatMessages { get; set; } = new List<TeamChatMessage>();

    public virtual ICollection<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
}
