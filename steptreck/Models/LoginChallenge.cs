using System;
using System.Collections.Generic;
using System.Net;

namespace steptreck.API.Models;

public partial class LoginChallenge
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = null!;

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
