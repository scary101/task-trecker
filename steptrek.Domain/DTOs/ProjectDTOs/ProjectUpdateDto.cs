using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.ProjectDTOs
{
    public class ProjectUpdateDto
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 60 символов")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Описание слишком длинное")]
        public string? Description { get; set; }

        [RegularExpression(@"^(|https?://.+)$", ErrorMessage = "Не верный формат URL")]
        [StringLength(200, ErrorMessage = "URL слишком длинный")]
        public string? GitUrl { get; set; }

        public bool? IsArchived { get; set; }
    }
}
