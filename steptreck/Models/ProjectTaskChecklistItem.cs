using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class ProjectTaskChecklistItem
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public string Title { get; set; } = null!;

    public bool IsDone { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ProjectTask Task { get; set; } = null!;
}
