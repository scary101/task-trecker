using steptreck.API.Models;

using Microsoft.EntityFrameworkCore;
using steptreck.Domain.DTOs;

namespace steptreck.API.Services.WorkUser
{
    public class BlockService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public BlockService(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task BlockByPassword(User user, CancellationToken ct = default)
        {

            if (user == null) throw new InvalidOperationException("Пользователь не найден");
            if (user.Attempt <= 5) return;

            var ban = new UserLock
            {
                UserId = user.Id,
                Reason = "Превышено количество попыток входа",
                UnlockAt = DateTime.UtcNow.AddMinutes(30)
            };

            await _context.UserLocks.AddAsync(ban, ct);
            await _context.SaveChangesAsync(ct);

            var orgId = await _context.Members
                .Where(m => m.UserId == user.Id)
                .Select(m => (int?)m.OrganizationId)
                .FirstOrDefaultAsync(ct);

            if (orgId.HasValue)
            {
                await _auditService.LogWithIdAsync(user.Id, orgId.Value, new AuditLogCreateDto
                {
                    Action = "create",
                    EntityType = "user_lock",
                    EntityId = ban.Id,
                    EntityName = user.CorporateEmail,
                    Title = "Пользователь заблокирован по паролю",
                    Description = ban.Reason,
                    NewValues = new { ban.Reason, ban.UnlockAt }
                }, ct);
            }
        }
    }
}
