using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class EmployeeWorkStatus
{
    public int UserId { get; set; }

    public int CurrentStatus { get; set; }

    public int? CurrentSessionId { get; set; }

    public DateTime StatusStartedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public int OrgId { get; set; }

    public virtual WorkSession? CurrentSession { get; set; }

    public virtual Organization Org { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
