using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class TaskDeadlineNotification
{
    public long Id { get; set; }

    public int TaskId { get; set; }

    public int MemberId { get; set; }

    public short HoursBefore { get; set; }

    public DateTime SentAtUtc { get; set; }
}
