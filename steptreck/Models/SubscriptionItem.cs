using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class SubscriptionItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal PricePerUnit { get; set; }

    public decimal MinQuantity { get; set; }

    public decimal MaxQuantity { get; set; }

    public decimal Step { get; set; }

    public string? Description { get; set; }
}
