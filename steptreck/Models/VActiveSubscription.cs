using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class VActiveSubscription
{
    public int? Id { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OrganizationId { get; set; }

    public int? PlanId { get; set; }

    public int? StatusId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Currency { get; set; }

    public int? PriceCents { get; set; }

    public int? MaxMembers { get; set; }

    public int? MaxTeams { get; set; }

    public int? MaxProjects { get; set; }

    public bool? AllowInvites { get; set; }

    public bool? AllowNewProjects { get; set; }

    public bool? AllowNewTeams { get; set; }

    public bool? IsCustom { get; set; }

    public string? Meta { get; set; }
}
