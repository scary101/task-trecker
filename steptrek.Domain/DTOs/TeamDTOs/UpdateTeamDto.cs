using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TeamDTOs
{
    public class UpdateTeamDto
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 60 символов")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Описание слишком длинное")]
        public string? Description { get; set; }
    }
}
