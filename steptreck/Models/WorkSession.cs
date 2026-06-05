using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class WorkSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public int DurationSeconds { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public int OrgId { get; set; }

    public int? ProjectId { get; set; }

    public int? TeamId { get; set; }

    public virtual ICollection<EmployeeWorkStatus> EmployeeWorkStatuses { get; set; } = new List<EmployeeWorkStatus>();

    public virtual Organization Org { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual ProjectTeam? Team { get; set; }

    public virtual User User { get; set; } = null!;
}
