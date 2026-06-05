using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;
using steptreck.Domain.Enums;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class CreateTaskForTeamDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Проект обязателен")]
        public int ProjectId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Команда обязательна")]
        public int? TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Исполнитель обязателен")]
        public int? AssignedToMemberId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 120 символов")]
        [RegularExpression(ValidationPatterns.ShortTitle, ErrorMessage = "Название содержит недопустимые символы")]
        public string Title { get; set; } = null!;

        [StringLength(2000, ErrorMessage = "Описание слишком длинное")]
        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        [Required(ErrorMessage = "Приоритет обязателен")]
        [EnumDataType(typeof(TaskPriority), ErrorMessage = "Недопустимый приоритет")]
        public TaskPriority Priority { get; set; }

        public List<CreateTaskChecklistItemDto>? Checklist { get; set; }
    }

    public sealed class CreateTaskDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Проект обязателен")]
        public int ProjectId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Команда обязательна")]
        public int? TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Участник обязателен")]
        public int? MebmerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Исполнитель обязателен")]
        public int? AssignedToMemberId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 120 символов")]
        [RegularExpression(ValidationPatterns.ShortTitle, ErrorMessage = "Название содержит недопустимые символы")]
        public string Title { get; set; } = null!;

        [StringLength(2000, ErrorMessage = "Описание слишком длинное")]
        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        [Required(ErrorMessage = "Приоритет обязателен")]
        [EnumDataType(typeof(TaskPriority), ErrorMessage = "Недопустимый приоритет")]
        public TaskPriority Priority { get; set; }

        public List<CreateTaskChecklistItemDto>? Checklist { get; set; }
    }
}
