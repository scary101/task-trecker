using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Notification
{
    public long Id { get; set; }

    public int MemberId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}
