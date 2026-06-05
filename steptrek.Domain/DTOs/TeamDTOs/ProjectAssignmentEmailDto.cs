using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TeamDTOs
{
    public class TeamAssignmentEmailDto
    {
        [Required(ErrorMessage = "Имя получателя обязательно")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 80 символов")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Имя содержит недопустимые символы")]
        public string RecipientFullName { get; set; } = null!;

        [Required(ErrorMessage = "Название команды обязательно")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 60 символов")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public string TeamName { get; set; } = null!;

        [Required(ErrorMessage = "Роль обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Роль должна быть от 2 до 50 символов")]
        [RegularExpression(ValidationPatterns.RoleName, ErrorMessage = "Роль содержит недопустимые символы")]
        public string RoleTitle { get; set; } = null!;

        [StringLength(60, ErrorMessage = "Название проекта слишком длинное")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public string? ProjectName { get; set; }
    }
}
