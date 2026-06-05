using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class TaskListFilterDto
    {
        public string? Status { get; set; }      
        public DateTime? DateFrom { get; set; } 
        public DateTime? DateTo { get; set; }    
    }

}
