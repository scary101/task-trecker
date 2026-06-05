using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class CreateTaskChecklistItemDto
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(80, MinimumLength = 1, ErrorMessage = "Название должно быть до 80 символов")]
        [RegularExpression(ValidationPatterns.ChecklistTitle, ErrorMessage = "Название содержит недопустимые символы")]
        public string Title { get; set; } = null!;
    }
}
