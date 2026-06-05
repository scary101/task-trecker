using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Invitation
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int OrganizationId { get; set; }

    public string CorporateEmail { get; set; } = null!;

    public int RoleId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual OrgRole Role { get; set; } = null!;
}
