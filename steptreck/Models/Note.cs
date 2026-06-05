using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Note
{
    public long Id { get; set; }

    public int MemberId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsPinned { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;
}
