using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class TeamChatMessage
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int OrganizationId { get; set; }

    public int TeamId { get; set; }

    public int SenderMemberId { get; set; }

    public string Text { get; set; } = null!;

    public bool IsPinned { get; set; }

    public DateTime? PinnedAt { get; set; }

    public int? PinnedByMemberId { get; set; }

    public long? ReplyToMessageId { get; set; }

    public virtual ICollection<TeamChatMessage> InverseReplyToMessage { get; set; } = new List<TeamChatMessage>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual Member? PinnedByMember { get; set; }

    public virtual TeamChatMessage? ReplyToMessage { get; set; }

    public virtual Member SenderMember { get; set; } = null!;

    public virtual ProjectTeam Team { get; set; } = null!;

    public virtual ICollection<TeamChatMessageReaction> TeamChatMessageReactions { get; set; } = new List<TeamChatMessageReaction>();
}
