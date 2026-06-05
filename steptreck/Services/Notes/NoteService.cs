using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.NoteDTOs;

namespace steptreck.API.Services.Notes
{
    public class NoteService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;


        public NoteService(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }
        public async Task<int> GetCurrentMemberId(CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            var memberId = await _context.Members
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(ct);

            if (memberId == 0)
                throw new UnauthorizedAccessException("Member not found for current user/org.");

            return memberId;
        }
        private async Task<Note> GetNoteBuId(int noteId, CancellationToken ct)
        {
            var memberId = await GetCurrentMemberId(ct);

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.MemberId == memberId && n.Id == noteId);

            if (note == null)
                throw new Exception("Заметка не найдена");

            return note;
        }

        public async Task<List<NoteListDto>> GetNoteListAsync(CancellationToken ct)
        {
            var memberId = await GetCurrentMemberId(ct);

            var notes = await _context.Notes
                .AsNoTracking()
                .Where(n => n.MemberId == memberId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .Select(n => new NoteListDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    IsPinned = n.IsPinned,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync(ct);

            return notes;
        }
        public async Task<NoteDto> GetNoteAsync(int noteId, CancellationToken ct)
        {
            var note = await GetNoteBuId(noteId, ct);

            return new NoteDto
            {
                Id = note.Id,
                Title = note.Title,
                IsPinned = note.IsPinned,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                Content = note.Content,
            };
        }

        public async Task CreateNote(string title, CancellationToken ct)
        {
            var memberId = await GetCurrentMemberId(ct);

            var note = new Note
            {
                MemberId = memberId,
                Title = title,
                IsPinned = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Content = ""
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(_userHelper.GetCurrentOrganizationId(), memberId, new AuditLogCreateDto
            {
                TargetMemberId = memberId,
                Action = "create",
                EntityType = "note",
                EntityId = note.Id,
                EntityName = note.Title,
                Title = "Создана заметка",
                NewValues = new { note.Title, note.IsPinned }
            }, ct);
        }
        public async Task SaveContentNote(string content, int noteId, CancellationToken ct)
        {
            var note = await GetNoteBuId(noteId, ct);
            var oldContent = note.Content;
            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(_userHelper.GetCurrentOrganizationId(), note.MemberId, new AuditLogCreateDto
            {
                TargetMemberId = note.MemberId,
                Action = "update",
                EntityType = "note",
                EntityId = note.Id,
                EntityName = note.Title,
                Title = "Обновлено содержимое заметки",
                OldValues = new { Content = oldContent },
                NewValues = new { note.Content }
            }, ct);
        }
        public async Task SaveTitleNote(string title, int noteId, CancellationToken ct)
        {
            var note = await GetNoteBuId(noteId, ct);
            var oldTitle = note.Title;
            note.Title = title;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(_userHelper.GetCurrentOrganizationId(), note.MemberId, new AuditLogCreateDto
            {
                TargetMemberId = note.MemberId,
                Action = "update",
                EntityType = "note",
                EntityId = note.Id,
                EntityName = note.Title,
                Title = "Переименована заметка",
                OldValues = new { Title = oldTitle },
                NewValues = new { note.Title }
            }, ct);
        }
        public async Task TooglePinnedNoteAsync(int noteId, CancellationToken ct)
        {
            var note = await GetNoteBuId(noteId, ct);
            if (note.IsPinned == true)
            {
                note.IsPinned = false;
            }
            else
            {
                note.IsPinned = true;
            }
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(_userHelper.GetCurrentOrganizationId(), note.MemberId, new AuditLogCreateDto
            {
                TargetMemberId = note.MemberId,
                Action = "update",
                EntityType = "note",
                EntityId = note.Id,
                EntityName = note.Title,
                Title = note.IsPinned ? "Заметка закреплена" : "Заметка откреплена",
                NewValues = new { note.IsPinned }
            }, ct);
        }
        public async Task DeleteNote(int noteId, CancellationToken ct)
        {
            var note = await GetNoteBuId(noteId, ct);
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(_userHelper.GetCurrentOrganizationId(), note.MemberId, new AuditLogCreateDto
            {
                TargetMemberId = note.MemberId,
                Action = "delete",
                EntityType = "note",
                EntityId = note.Id,
                EntityName = note.Title,
                Title = "Удалена заметка",
                OldValues = new { note.Title, note.IsPinned }
            }, ct);
        }

    }
}
