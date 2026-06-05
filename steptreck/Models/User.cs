using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class User
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CorporateEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public string Salt { get; set; } = null!;

    public int? Attempt { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string? Patronymic { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<ConfirmationCode> ConfirmationCodes { get; set; } = new List<ConfirmationCode>();

    public virtual EmployeeWorkStatus? EmployeeWorkStatus { get; set; }

    public virtual ICollection<LoginChallenge> LoginChallenges { get; set; } = new List<LoginChallenge>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual PasswordReset? PasswordReset { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<UserLock> UserLocks { get; set; } = new List<UserLock>();

    public virtual ICollection<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
}
