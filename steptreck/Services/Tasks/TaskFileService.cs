using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.File;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.TaskDTOs.TaskFileDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Tasks
{
    public class TaskFileService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly FileManagerHelper _storage;
        private readonly AuditService _auditService;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

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
                throw new UnauthorizedAccessException("Руководителю недоступны файлы задач");
        }

        public TaskFileService(
            AppDbContext context,
            UserHelper userHelper,
            FileManagerHelper storage,
            AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _storage = storage;
            _auditService = auditService;
        }

        public async Task<TaskFileReadDto> AddTaskFileAsync(
            int taskId,
            IFormFile file,
            CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            if (file is null)
                throw new ArgumentNullException(nameof(file), "Файл не передан");

            if (file.Length <= 0)
                throw new ArgumentException("Файл пустой или повреждён");

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"Файл слишком большой. Максимум: {MaxFileSizeBytes / (1024 * 1024)} МБ");

            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();

            var taskExists = await _context.ProjectTasks
                .AnyAsync(t =>
                    t.Id == taskId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    (t.TeamId == null || t.Team!.IsActive) &&
                    !t.IsArchived, ct);

            if (!taskExists)
                throw new KeyNotFoundException("Задача не найден или у вас нет к нему доступа");

            var safeFileName = NormalizeFileName(file.FileName);
            var objectKey = BuildTaskStorageKey(orgId, taskId, safeFileName);

            await using var stream = file.OpenReadStream();
            await _storage.UploadAsync(objectKey, stream, file.ContentType, ct);

            var attachment = new Attachment
            {
                CreatedAt = DateTime.UtcNow,
                OrganizationId = orgId,
                TaskId = taskId,
                TeamId = null,
                ProjectId = null,
                UploadedByUserId = userId,

                FileName = safeFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                StorageKey = objectKey
            };

            _context.Attachments.Add(attachment);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                try { await _storage.DeleteAsync(objectKey, ct); } catch { }
                throw;
            }

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                TaskId = taskId,
                Action = "create",
                EntityType = "task_file",
                EntityId = attachment.Id,
                EntityName = attachment.FileName,
                Title = "Файл добавлен к задаче",
                NewValues = new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }
            }, ct);

            return ToReadDto(attachment);
        }

        public async Task<List<TaskFileReadDto>> GetTaskFilesAsync(
            int taskId,
            CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            var orgId = _userHelper.GetCurrentOrganizationId();

            var files = await _context.Attachments
                .AsNoTracking()
                .Where(a =>
                    a.OrganizationId == orgId &&
                    a.TaskId == taskId &&
                    a.Task != null &&
                    !a.Task.IsArchived &&
                    !a.Task.Project.IsArchived &&
                    (a.Task.TeamId == null || a.Task.Team!.IsActive))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new TaskFileReadDto
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    OrganizationId = a.OrganizationId,
                    TaskId = a.TaskId ?? 0,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes,
                    StorageKey = a.StorageKey
                })
                .ToListAsync(ct);

            return files;
        }

        private static TaskFileReadDto ToReadDto(Attachment a) => new()
        {
            Id = a.Id,
            CreatedAt = a.CreatedAt,
            OrganizationId = a.OrganizationId,
            TaskId = a. TaskId ?? 0,
            FileName = a.FileName,
            ContentType = a.ContentType,
            SizeBytes = a.SizeBytes,
            StorageKey = a.StorageKey
        };

        private static string BuildTaskStorageKey(int orgId, int taskId, string fileName)
        {
            var ext = Path.GetExtension(fileName);
            var guid = Guid.NewGuid().ToString("N");
            var datePart = DateTime.UtcNow.ToString("yyyy-MM");
            return $"org-{orgId}/tasks/{taskId}/{datePart}/{guid}{ext}";
        }     

        private static string NormalizeFileName(string fileName)
        {
            fileName = (fileName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            fileName = Path.GetFileName(fileName);

            const int maxLen = 200;
            if (fileName.Length > maxLen)
            {
                var ext = Path.GetExtension(fileName);
                var name = Path.GetFileNameWithoutExtension(fileName);
                name = name.Substring(0, Math.Min(name.Length, maxLen - ext.Length));
                fileName = name + ext;
            }

            return fileName;
        }
        public async Task<TaskFileDownloadDto> DownloadTaskFileAsync(
            int attachmentId,
            CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            var orgId = _userHelper.GetCurrentOrganizationId();
            var attachment = await _context.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.OrganizationId == orgId &&
                    a.TaskId != null &&
                    a.Task != null &&
                    !a.Task.IsArchived &&
                    !a.Task.Project.IsArchived &&
                    (a.Task.TeamId == null || a.Task.Team!.IsActive),
                    ct);

            if (attachment is null)
                throw new KeyNotFoundException("Файл не найден или у вас нет к нему доступа");

            if (string.IsNullOrWhiteSpace(attachment.StorageKey))
                throw new InvalidOperationException("У файла отсутствует ключ хранения (StorageKey)");

            var stream = await _storage.DownloadAsync(attachment.StorageKey, ct);

            return new TaskFileDownloadDto
            {
                FileName = attachment.FileName,
                ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType,
                Content = stream
            };
        }

        public async Task DeleteTaskFileAsync(int attachmentId, CancellationToken ct = default)
        {
            await EnsureCurrentUserIsNotOwnerAsync(ct);

            var orgId = _userHelper.GetCurrentOrganizationId();

            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.OrganizationId == orgId &&
                    a.TaskId != null &&
                    a.Task != null &&
                    !a.Task.IsArchived &&
                    !a.Task.Project.IsArchived &&
                    (a.Task.TeamId == null || a.Task.Team!.IsActive),
                    ct);

            if (attachment is null)
                throw new KeyNotFoundException("Файл не найден или у вас нет к нему доступа");

            var storageKey = attachment.StorageKey;

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TaskId = attachment.TaskId,
                Action = "delete",
                EntityType = "task_file",
                EntityId = attachment.Id,
                EntityName = attachment.FileName,
                Title = "Файл удален из задачи",
                OldValues = new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }
            }, ct);

            if (!string.IsNullOrWhiteSpace(storageKey))
            {
                try { await _storage.DeleteAsync(storageKey, ct); } catch { }
            }
        }
    }
}
