using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public class PutCheckListItem
    {
        [Range(1, int.MaxValue, ErrorMessage = "Пункт обязателен")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(80, MinimumLength = 1, ErrorMessage = "Название должно быть до 80 символов")]
        [RegularExpression(ValidationPatterns.ChecklistTitle, ErrorMessage = "Название содержит недопустимые символы")]
        public string Name { get; set; }
    }
}
