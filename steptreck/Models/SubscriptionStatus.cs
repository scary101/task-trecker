using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class SubscriptionStatus
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
