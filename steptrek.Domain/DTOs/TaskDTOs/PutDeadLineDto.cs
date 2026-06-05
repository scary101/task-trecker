using System.ComponentModel.DataAnnotations;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public class PutDeadLineDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Задача обязательна")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Дата обязательна")]
        public DateTime Date {  get; set; }
    }
}
