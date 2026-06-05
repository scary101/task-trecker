using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Plan
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Name { get; set; } = null!;

    public int MinUsers { get; set; }

    public int MaxUsers { get; set; }

    public string? Description { get; set; }

    public string Currency { get; set; } = null!;

    public int BasePriceCents { get; set; }

    public int MaxTeams { get; set; }

    public int MaxProjects { get; set; }

    public bool AllowInvites { get; set; }

    public bool AllowNewProjects { get; set; }

    public bool AllowNewTeams { get; set; }

    public int MinTeams { get; set; }

    public int MinProjects { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
