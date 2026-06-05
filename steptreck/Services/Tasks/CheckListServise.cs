using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.TaskDTOs;
using steptreck.Domain.DTOs.TaskDTOs.CheckListDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Tasks
{
    public class CheckListServise
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;

        public CheckListServise(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }

        private static DateTime UtcNow() => DateTime.UtcNow;

        private async Task EnsureCurrentUserIsNotOwnerAsync(CancellationToken ct)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();

            var roleId = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.UserId == userId && m.IsActive)
                .Select(m => (int?)m.RoleId)
                .FirstOrDefaultAsync(ct);

            if (roleId == (int)RoleEnum.Owner)
                throw new UnauthorizedAccessException("Руководителю недоступны чек-листы задач");
        }

        public async Task CompleteNextChecklistItemAsync(int taskId, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();
            var item = await _context.ProjectTaskChecklistItems
                .Include(i => i.Task)
                .Where(i =>
                    i.TaskId == taskId &&
                    !i.IsDone &&
                    i.Task.Project.OrganizationId == orgId &&
                    !i.Task.Project.IsArchived &&
                    (i.Task.TeamId == null || i.Task.Team!.IsActive) &&
                    !i.Task.IsArchived
                )
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync(ct);

            if (item == null)
                throw new InvalidOperationException("Все пункты чек-листа уже выполнены");

            item.IsDone = true;
            item.CompletedAt = UtcNow();

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = item.Task.ProjectId,
                TeamId = item.Task.TeamId,
                TaskId = item.TaskId,
                Action = "update",
                EntityType = "task_checklist_item",
                EntityId = item.Id,
                EntityName = item.Title,
                Title = "Пункт чек-листа выполнен",
                NewValues = new { item.IsDone, item.CompletedAt }
            }, ct);
        }
        public async Task PutItem(PutCheckListItem model, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var item = await _context.ProjectTaskChecklistItems
                .Include(i => i.Task)
                .FirstOrDefaultAsync(i =>
                    i.Id == model.Id &&
                    i.Task.Project.OrganizationId == orgId &&
                    !i.Task.Project.IsArchived &&
                    (i.Task.TeamId == null || i.Task.Team!.IsActive) &&
                    !i.Task.IsArchived, ct);

            if (item == null)
                throw new InvalidOperationException("Пункт чек-листа не найден");

            var oldTitle = item.Title;
            item.Title = model.Name!.Trim();

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = item.Task.ProjectId,
                TeamId = item.Task.TeamId,
                TaskId = item.TaskId,
                Action = "update",
                EntityType = "task_checklist_item",
                EntityId = item.Id,
                EntityName = item.Title,
                Title = "Пункт чек-листа переименован",
                OldValues = new { Title = oldTitle },
                NewValues = new { item.Title }
            }, ct);
        }
        public async Task SaveChecklistAsync(SaveChecklistDto dto, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            int orgId = _userHelper.GetCurrentOrganizationId();

            var task = await _context.ProjectTasks
                .Where(t =>
                    t.Id == dto.TaskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived)
                .FirstOrDefaultAsync(ct);

            if (task is null)
                throw new InvalidOperationException("Задача не найдена или нет доступа");

            var existing = await _context.ProjectTaskChecklistItems
                .Where(i => i.TaskId == dto.TaskId)
                .ToListAsync(ct);

            var incoming = dto.Items
                .Select((x, idx) => new
                {
                    Id = x.Id.HasValue && x.Id.Value > 0 ? x.Id.Value : (int?)null,
                    Title = x.Title?.Trim(),
                    SortOrder = idx + 1
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Title))
                .ToList();

            var dup = incoming.Where(x => x.Id.HasValue)
                              .GroupBy(x => x.Id!.Value)
                              .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                throw new InvalidOperationException($"Дублируется пункт чек-листа Id={dup.Key}");

            var incomingIds = incoming.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
            var existingById = existing.ToDictionary(x => x.Id);

            var toDelete = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
            if (toDelete.Count > 0)
                _context.ProjectTaskChecklistItems.RemoveRange(toDelete);

            foreach (var it in incoming)
            {
                if (it.Id.HasValue)
                {
                    if (!existingById.TryGetValue(it.Id.Value, out var entity))
                        throw new InvalidOperationException($"Пункт чек-листа Id={it.Id.Value} не найден");

                    entity.Title = it.Title!;
                    entity.SortOrder = it.SortOrder;
                }
                else
                {
                    _context.ProjectTaskChecklistItems.Add(new ProjectTaskChecklistItem
                    {
                        TaskId = dto.TaskId,
                        Title = it.Title!,
                        SortOrder = it.SortOrder,
                        IsDone = false,
                        CreatedAt = UtcNow(),
                        CompletedAt = null
                    });
                }
            }

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = task.ProjectId,
                TeamId = task.TeamId,
                TaskId = task.Id,
                Action = "update",
                EntityType = "task_checklist",
                EntityId = task.Id,
                EntityName = task.Title,
                Title = "Чек-лист задачи сохранен",
                NewValues = new { ItemsCount = incoming.Count, DeletedCount = toDelete.Count }
            }, ct);
        }
    }



}
