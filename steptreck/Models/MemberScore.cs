using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class MemberScore
{
    public long Id { get; set; }

    public int MemberId { get; set; }

    public int CompletedCount { get; set; }

    public int MissedCount { get; set; }

    public int TotalAssignedCount { get; set; }

    public decimal Trust { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual Member Member { get; set; } = null!;
}
