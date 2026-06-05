using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public string TableName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public long? RecordId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime CreatedAt { get; set; }
}
