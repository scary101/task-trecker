using System;
using System.Collections.Generic;

namespace steptreck.API.Models;

public partial class CalendarEventType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public short SortOrder { get; set; }
}
