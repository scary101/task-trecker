using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class ProjectTeamMember
{
    public int TeamId { get; set; }

    public int MemberId { get; set; }

    public string? TeamRole { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual ProjectTeam Team { get; set; } = null!;
}
