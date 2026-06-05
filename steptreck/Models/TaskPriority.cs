using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class TaskPriority
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public short SortOrder { get; set; }

    public decimal MissPenalty { get; set; }

    public decimal DoneReward { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
}
