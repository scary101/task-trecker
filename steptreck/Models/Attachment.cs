using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Attachment
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int OrganizationId { get; set; }

    public int? ProjectId { get; set; }

    public int? TeamId { get; set; }

    public int? UploadedByUserId { get; set; }

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public string StorageKey { get; set; } = null!;

    public int? TaskId { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual ProjectTask? Task { get; set; }

    public virtual ProjectTeam? Team { get; set; }

    public virtual User? UploadedByUser { get; set; }
}
