using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Project
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? GitUrl { get; set; }

    public bool IsArchived { get; set; }

    public int? CreatedByUserId { get; set; }

    public string? CardBackgroundUrl { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual User? CreatedByUser { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTeam> ProjectTeams { get; set; } = new List<ProjectTeam>();

    public virtual ICollection<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
}
