using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class TeamChatMessageReaction
{
    public int Id { get; set; }

    public long MessageId { get; set; }

    public int MemberId { get; set; }

    public string Emoji { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual TeamChatMessage Message { get; set; } = null!;
}
