using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class OrgRole
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
