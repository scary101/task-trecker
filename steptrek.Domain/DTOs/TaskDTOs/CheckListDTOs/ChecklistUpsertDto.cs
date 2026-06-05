using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TaskDTOs.CheckListDTOs
{
    public class ChecklistUpsertDto
    {
        public Guid UiId { get; set; } = Guid.NewGuid();
        public int? Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(80, MinimumLength = 1, ErrorMessage = "Название должно быть до 80 символов")]
        [RegularExpression(ValidationPatterns.ChecklistTitle, ErrorMessage = "Название содержит недопустимые символы")]
        public string? Title { get; set; }
    }
}
