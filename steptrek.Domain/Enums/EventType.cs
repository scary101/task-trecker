using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.Enums
{
    public enum TypeEvent : short
    {
        TaskCreated = 1,
        TaskDeadline = 2,
        TaskCompleted = 3,
        Meeting = 4,
        ImportantDate = 5
    }
}
