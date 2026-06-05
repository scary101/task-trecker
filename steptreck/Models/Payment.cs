using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Payment
{
    public long Id { get; set; }

    public int OrganizationId { get; set; }

    public int? SubscriptionId { get; set; }

    public int AmountCents { get; set; }

    public string Currency { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string? ExternalId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public string? Meta { get; set; }

    public string? ReceiptObjectKey { get; set; }

    public string? ReceiptContentType { get; set; }

    public DateTime? ReceiptCreatedAt { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual Subscription? Subscription { get; set; }
}
