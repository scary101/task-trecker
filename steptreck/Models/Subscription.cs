using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Subscription
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int OrganizationId { get; set; }

    public int? PlanId { get; set; }

    public int StatusId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Currency { get; set; } = null!;

    public int PriceCents { get; set; }

    public int MaxMembers { get; set; }

    public int MaxTeams { get; set; }

    public int MaxProjects { get; set; }

    public bool AllowInvites { get; set; }

    public bool AllowNewProjects { get; set; }

    public bool AllowNewTeams { get; set; }

    public string? Meta { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Plan? Plan { get; set; }

    public virtual SubscriptionStatus Status { get; set; } = null!;
}
