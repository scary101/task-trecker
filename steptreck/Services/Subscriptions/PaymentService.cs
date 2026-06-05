using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.SubscriptionsDTOs;

namespace steptreck.API.Services.Subscriptions
{
    public class PaymentService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;

        public PaymentService(AppDbContext context, UserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        private static PaymentReadDto ToDto(Payment p) => new PaymentReadDto
        {
            Id = p.Id,
            OrganizationId = p.OrganizationId,
            SubscriptionId = p.SubscriptionId,
            AmountCents = p.AmountCents,
            Currency = p.Currency,
            Provider = p.Provider,
            Status = p.Status,
            Reason = p.Reason,
            ExternalId = p.ExternalId,
            CreatedAt = p.CreatedAt,
            PaidAtUtc = p.PaidAtUtc,
            Meta = p.Meta,
            ReceiptObjectKey = p.ReceiptObjectKey,
            ReceiptContentType = p.ReceiptContentType,
            ReceiptCreatedAt = p.ReceiptCreatedAt
        };
        public async Task<IReadOnlyList<PaymentReadDto>> GetListAsync(
            int page = 1,
            int pageSize = 20,
            string? status = null,
            string? provider = null,
            int? subscriptionId = null,
            CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var orgId = _userHelper.GetCurrentOrganizationId();

            var q = _context.Payments
                .AsNoTracking()
                .Where(p => p.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(provider))
                q = q.Where(p => p.Provider == provider);

            if (subscriptionId.HasValue)
                q = q.Where(p => p.SubscriptionId == subscriptionId.Value);

            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => ToDto(p))
                .ToListAsync(ct);

            return items;
        }

        public async Task<PaymentReadDto> GetByIdAsync(long paymentId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var p = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == paymentId && x.OrganizationId == orgId, ct);

            if (p == null)
                throw new KeyNotFoundException("Платёж не найден.");

            return ToDto(p);
        }
    }
}
