using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class Member
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int OrganizationId { get; set; }

    public int? UserId { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Username { get; set; }

    public virtual MemberScore? MemberScore { get; set; }

    public virtual ICollection<MemberScoreLog> MemberScoreLogs { get; set; } = new List<MemberScoreLog>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<ProjectCalendarEvent> ProjectCalendarEvents { get; set; } = new List<ProjectCalendarEvent>();

    public virtual ICollection<ProjectTask> ProjectTaskAssignedToMembers { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTask> ProjectTaskCreatedByMembers { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTeamMember> ProjectTeamMembers { get; set; } = new List<ProjectTeamMember>();

    public virtual ICollection<PushToken> PushTokens { get; set; } = new List<PushToken>();

    public virtual OrgRole Role { get; set; } = null!;

    public virtual ICollection<TeamChatMessage> TeamChatMessagePinnedByMembers { get; set; } = new List<TeamChatMessage>();

    public virtual ICollection<TeamChatMessageReaction> TeamChatMessageReactions { get; set; } = new List<TeamChatMessageReaction>();

    public virtual ICollection<TeamChatMessage> TeamChatMessageSenderMembers { get; set; } = new List<TeamChatMessage>();

    public virtual User? User { get; set; }
}
