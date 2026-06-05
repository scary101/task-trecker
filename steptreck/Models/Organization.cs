using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Organization
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<EmployeeWorkStatus> EmployeeWorkStatuses { get; set; } = new List<EmployeeWorkStatus>();

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<TeamChatMessage> TeamChatMessages { get; set; } = new List<TeamChatMessage>();

    public virtual ICollection<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
}
