using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class MemberScoreLog
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public decimal Delta { get; set; }

    public bool IsIncrease { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Member Member { get; set; } = null!;
}
