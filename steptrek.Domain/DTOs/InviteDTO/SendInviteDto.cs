using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.InviteDTO
{
    public class SendInviteDto
    {
        [Required(ErrorMessage = "Имя отправителя обязательно")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Имя отправителя должно быть от 2 до 80 символов")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Имя отправителя содержит недопустимые символы")]
        public string FullNameSender {  get; set; }

        [Required(ErrorMessage = "Имя получателя обязательно")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Имя получателя должно быть от 2 до 80 символов")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Имя получателя содержит недопустимые символы")]
        public string FullNameRecipient { get; set; }

        [Required(ErrorMessage = "Название организации обязательно")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 60 символов")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public string OrgName {  get; set; }
    }
}
