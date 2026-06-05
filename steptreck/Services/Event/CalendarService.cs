using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.CalendarDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Event
{
    public class CalendarService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;


        public CalendarService(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }

        private static TypeEvent ParseManualEventType(string type)
        {
            if (!Enum.TryParse<TypeEvent>(type, true, out var eventType))
                throw new Exception("Неверный тип события");

            if (eventType is TypeEvent.TaskCreated
                or TypeEvent.TaskDeadline
                or TypeEvent.TaskCompleted)
                throw new Exception("Нельзя создавать системные события вручную");

            return eventType;
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static DateTime? ToUtc(DateTime? value)
        {
            return value.HasValue ? ToUtc(value.Value) : null;
        }

        private async Task EnsureCanCreateManualEventAsync(int memberId, int orgId, CancellationToken ct)
        {
            var roleId = await _context.Members
                .AsNoTracking()
                .Where(m => m.Id == memberId && m.OrganizationId == orgId)
                .Select(m => m.RoleId)
                .FirstOrDefaultAsync(ct);

            if (roleId == (int)RoleEnum.Employee)
                throw new UnauthorizedAccessException("Обычный сотрудник не может создавать события календаря.");
        }


        public async Task<List<CalendarEventDto>> GetAllEventAsync(int teamId, CancellationToken ct)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var teamExists = await _context.ProjectTeams
                .AsNoTracking()
                .AnyAsync(t =>
                    t.Id == teamId &&
                    t.Project.OrganizationId == orgId &&
                    t.IsActive &&
                    !t.Project.IsArchived, ct);

            if (!teamExists)
                return new List<CalendarEventDto>();

            var tasks = await _context.ProjectTasks
                .Where(t =>
                    t.TeamId == teamId &&
                    !t.IsArchived &&
                    !t.Project.IsArchived &&
                    t.Team != null &&
                    t.Team.IsActive)
                .ToListAsync(ct);

            var taskEvents = tasks
                .SelectMany(t => new CalendarEventDto?[]
                {
            new()
            {
                Id = $"task-created-{t.Id}",
                TaskId = t.Id,
                Title = $"Создана задача: {t.Title}",
                Type = TypeEvent.TaskCreated.ToString(),
                StartAt = t.CreatedAt,
                IsPinned = false,
                Priority = t.Priority,
                Status = t.Status
            },

            t.Deadline.HasValue ? new CalendarEventDto
            {
                Id = $"task-deadline-{t.Id}",
                TaskId = t.Id,
                Title = $"Дедлайн: {t.Title}",
                Type = TypeEvent.TaskDeadline.ToString(),
                StartAt = t.Deadline.Value,
                IsPinned = false,
                Priority = t.Priority,
                Status = t.Status
            } : null,

            t.CompletedAt.HasValue ? new CalendarEventDto
            {
                Id = $"task-completed-{t.Id}",
                TaskId = t.Id,
                Title = $"Завершена задача: {t.Title}",
                Type = TypeEvent.TaskCompleted.ToString(),
                StartAt = t.CompletedAt.Value,
                IsPinned = false,
                Priority = t.Priority,
                Status = t.Status
            } : null
                })
                .Where(e => e != null)
                .Cast<CalendarEventDto>()
                .ToList();

            var manualEvents = await _context.ProjectCalendarEvents
                .Where(e =>
                    e.TeamId == teamId &&
                    e.Team.IsActive &&
                    !e.Team.Project.IsArchived)
                .Select(e => new CalendarEventDto
                {
                    Id = $"event-{e.Id}",
                    TaskId = e.TaskId,
                    Title = e.Title,
                    Type = ((TypeEvent)e.TypeId).ToString(),
                    StartAt = e.StartAt,
                    EndAt = e.EndAt,
                    IsPinned = e.IsPinned,
                    Priority = null,
                    Status = null
                })
                .ToListAsync(ct);

            return taskEvents
                .Concat(manualEvents)
                .OrderByDescending(e => e.IsPinned)
                .ThenBy(e => e.StartAt)
                .ToList();
        }
        public async Task<ProjectCalendarEvent> CreateEventAsync(
            CreateProjectCalendarEventDto dto,
            CancellationToken ct)
        {
            var startAtUtc = ToUtc(dto.StartAt);
            var endAtUtc = ToUtc(dto.EndAt);

            if (dto.StartAt.Date < DateTime.Today)
                throw new Exception("Нельзя создать событие раньше текущей даты");

            if (endAtUtc.HasValue && endAtUtc.Value < startAtUtc)
                throw new Exception("Дата окончания не может быть раньше даты начала");

            var mebmerId = await _userHelper.GetCurrentMemberId();
            var orgId = _userHelper.GetCurrentOrganizationId();
            await EnsureCanCreateManualEventAsync(mebmerId, orgId, ct);

            var eventType = ParseManualEventType(dto.Type);

            var teamExists = await _context.ProjectTeams
                .AsNoTracking()
                .AnyAsync(t =>
                    t.Id == dto.TeamId &&
                    t.Project.OrganizationId == orgId &&
                    t.IsActive &&
                    !t.Project.IsArchived, ct);

            if (!teamExists)
                throw new Exception("Команда не найдена или недоступна");

            var calendarEvent = new ProjectCalendarEvent
            {
                TeamId = dto.TeamId,
                CreatedByMemberId = mebmerId,
                Title = dto.Title,
                TypeId = (short)eventType,
                StartAt = startAtUtc,
                EndAt = endAtUtc,
                Description = dto.Description,
                IsPinned = dto.IsPinned,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.ProjectCalendarEvents.Add(calendarEvent);

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, mebmerId, new AuditLogCreateDto
            {
                TeamId = calendarEvent.TeamId,
                TaskId = calendarEvent.TaskId,
                Action = "create",
                EntityType = "calendar_event",
                EntityId = calendarEvent.Id,
                EntityName = calendarEvent.Title,
                Title = "Создано событие календаря",
                NewValues = new
                {
                    calendarEvent.Title,
                    Type = eventType.ToString(),
                    calendarEvent.StartAt,
                    calendarEvent.EndAt,
                    calendarEvent.Description,
                    calendarEvent.IsPinned
                }
            });

            return calendarEvent;
        }
        public async Task<ProjectCalendarEvent> UpdateEventAsync(
            int eventId,
            UpdateProjectCalendarEventDto dto,
            CancellationToken ct)
        {
            var startAtUtc = ToUtc(dto.StartAt);
            var endAtUtc = ToUtc(dto.EndAt);

            if (endAtUtc.HasValue && endAtUtc.Value < startAtUtc)
                throw new Exception("Дата окончания не может быть раньше даты начала");

            TypeEvent eventType = ParseManualEventType(dto.Type);
            var actorMemberId = await _userHelper.GetCurrentMemberId(ct);
            var orgId = _userHelper.GetCurrentOrganizationId();

            var calendarEvent = await _context.ProjectCalendarEvents
                .FirstOrDefaultAsync(e =>
                    e.Id == eventId &&
                    e.Team.Project.OrganizationId == orgId &&
                    e.Team.IsActive &&
                    !e.Team.Project.IsArchived, ct);

            if (calendarEvent == null)
                throw new Exception("Событие не найдено");

            var oldValues = new
            {
                calendarEvent.TaskId,
                calendarEvent.Title,
                TypeId = calendarEvent.TypeId,
                calendarEvent.StartAt,
                calendarEvent.EndAt,
                calendarEvent.Description,
                calendarEvent.IsPinned
            };

            calendarEvent.TaskId = dto.TaskId;
            calendarEvent.Title = dto.Title;
            calendarEvent.TypeId = (short)eventType;
            calendarEvent.StartAt = startAtUtc;
            calendarEvent.EndAt = endAtUtc;
            calendarEvent.Description = dto.Description;
            calendarEvent.IsPinned = dto.IsPinned;
            calendarEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, actorMemberId, new AuditLogCreateDto
            {
                TeamId = calendarEvent.TeamId,
                TaskId = calendarEvent.TaskId,
                Action = "update",
                EntityType = "calendar_event",
                EntityId = calendarEvent.Id,
                EntityName = calendarEvent.Title,
                Title = "Обновлено событие календаря",
                OldValues = oldValues,
                NewValues = new
                {
                    calendarEvent.TaskId,
                    calendarEvent.Title,
                    Type = eventType.ToString(),
                    calendarEvent.StartAt,
                    calendarEvent.EndAt,
                    calendarEvent.Description,
                    calendarEvent.IsPinned
                }
            }, ct);

            return calendarEvent;
        }
        public async Task DeleteEventAsync(int eventId, CancellationToken ct)
        {
            var actorMemberId = await _userHelper.GetCurrentMemberId(ct);
            var orgId = _userHelper.GetCurrentOrganizationId();

            var calendarEvent = await _context.ProjectCalendarEvents
                .FirstOrDefaultAsync(e =>
                    e.Id == eventId &&
                    e.Team.Project.OrganizationId == orgId &&
                    e.Team.IsActive &&
                    !e.Team.Project.IsArchived, ct);

            if (calendarEvent == null)
                throw new Exception("Событие не найдено");

            _context.ProjectCalendarEvents.Remove(calendarEvent);

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, actorMemberId, new AuditLogCreateDto
            {
                TeamId = calendarEvent.TeamId,
                TaskId = calendarEvent.TaskId,
                Action = "delete",
                EntityType = "calendar_event",
                EntityId = calendarEvent.Id,
                EntityName = calendarEvent.Title,
                Title = "Удалено событие календаря",
                OldValues = new
                {
                    calendarEvent.Title,
                    TypeId = calendarEvent.TypeId,
                    calendarEvent.StartAt,
                    calendarEvent.EndAt,
                    calendarEvent.Description,
                    calendarEvent.IsPinned
                }
            }, ct);
        }
    }
}
