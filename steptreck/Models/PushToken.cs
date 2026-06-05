using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class PushToken
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public string Token { get; set; } = null!;

    public string? Platform { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;
}
