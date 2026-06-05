using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.TaskDTOs;

namespace steptreck.API.Services.Notifications
{
    public class NotificationsHelper
    {
        private readonly IConfiguration _configuration;
        public NotificationsHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string TaskCreated(CreateTaskForTeamDto dto, string? priorityTitle = null)
        {
            var parts = new List<string>();

            parts.Add($"Для Вас создана новая задача: «{Safe(dto.Title)}».");

            var priorityText = priorityTitle ?? GetPriorityTitle(dto.Priority);
            if (!string.IsNullOrWhiteSpace(priorityText))
                parts.Add($"Приоритет: {priorityText}.");

            if (dto.Deadline.HasValue)
                parts.Add($"Срок выполнения: {dto.Deadline.Value:dd.MM.yyyy}.");

            var desc = (dto.Description ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(desc))
                parts.Add($"Описание: {Short(desc, 180)}");

            return string.Join(" ", parts);
        }

        public string TaskDeadlineChanged(string taskTitle, DateTime? deadlineUtc)
        {
            if (deadlineUtc is null)
                return $"Для задачи «{Safe(taskTitle)}» срок выполнения был снят.";

            return $"Для задачи «{Safe(taskTitle)}» установлен/изменён срок выполнения: {deadlineUtc.Value:dd.MM.yyyy}.";
        }

        public string TaskCompleted(string taskTitle)
        {
            return $"Задача «{Safe(taskTitle)}» отмечена как выполненная.";
        }

        public string TaskUpdated(string taskTitle)
        {
            return $"Данные задачи «{Safe(taskTitle)}» были обновлены.";
        }
        public string DeadlineTime(string taskTitle, int hour)
        {
            return $"Задачу «{Safe(taskTitle)}» нужно сдать через (часов) {hour}";
        }
        public WarmUpDto WarmUp()
        {
            var frontendUrl = _configuration["App:FrontendUrl"];

            return new WarmUpDto
            {
                Message = "Прошло уже 45 минут! Пора размяться!",
                FileUrl = $"{frontendUrl}/doc/warmup.pdf"
            };
        }

        private static string Safe(string? s) => (s ?? "").Trim();

        private static string Short(string s, int max)
        {
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            if (s.Length <= max) return s;
            return s.Substring(0, max).TrimEnd() + "…";
        }

        private static string? GetPriorityTitle(Domain.Enums.TaskPriority priority)
        {
            return priority switch
            {
                Domain.Enums.TaskPriority.Low => "Низкий",
                Domain.Enums.TaskPriority.Medium => "Обычный",
                Domain.Enums.TaskPriority.High => "Высокий",
                Domain.Enums.TaskPriority.Critical => "Критический",
                _ => null
            };
        }
    }
}
