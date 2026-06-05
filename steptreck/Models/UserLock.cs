using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class UserLock
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime LockedAt { get; set; }

    public DateTime? UnlockAt { get; set; }

    public virtual User User { get; set; } = null!;
}
