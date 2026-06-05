using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.Constants;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.SubscriptionsDTOs;
using steptreck.Domain.Enums;
using System.Numerics;
using System.Reactive.Joins;

namespace steptreck.API.Services.Subscriptions
{
    public class SubscriptionService
    {
        private readonly AppDbContext _conetxet;
        private readonly UserHelper _userHelper;
        private readonly ReceiptPdfService _receiptPdf;
        private readonly AuditService _auditService;

        public SubscriptionService(AppDbContext conetxet, UserHelper userHelper, ReceiptPdfService receiptPdf, AuditService auditService)
        {
            _conetxet = conetxet;
            _userHelper = userHelper;
            _receiptPdf = receiptPdf;
            _auditService = auditService;
        }

        private static void ApplyPlanSnapshotToSubscription(Subscription sub, Plan plan)
        {
            sub.Currency = plan.Currency;
            sub.PriceCents = plan.BasePriceCents;

            sub.MaxMembers = plan.MaxUsers;
            sub.MaxTeams = plan.MaxTeams;
            sub.MaxProjects = plan.MaxProjects;

            sub.AllowInvites = plan.AllowInvites;
            sub.AllowNewProjects = plan.AllowNewProjects;
            sub.AllowNewTeams = plan.AllowNewTeams;
        }

        private async Task EnsureOwnerAsync(int orgId, int userId, CancellationToken ct)
        {
            var isOwner = await _conetxet.Members
                .AsNoTracking()
                .AnyAsync(m =>
                    m.OrganizationId == orgId &&
                    m.UserId == userId &&
                    m.RoleId == (int)RoleEnum.Owner,
                    ct);

            if (!isOwner)
                throw new InvalidOperationException("Только владелец организации может управлять подпиской.");
        }

        private async Task<int> GetStatusIdByCodeAsync(string code, CancellationToken ct)
        {
            var id = await _conetxet.SubscriptionStatuses
                .AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (id == 0)
                throw new InvalidOperationException($"Статус подписки '{code}' не найден.");

            return id;
        }

        private async Task CheckActiveSubscribtionAsync(int orgId, int code,  CancellationToken ct)
        {
            var hasActive = await _conetxet.Subscriptions
                .AsNoTracking()
                .AnyAsync(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == code &&
                    s.EndDate != null &&
                    s.EndDate > DateTime.UtcNow,
                    ct);

            if (hasActive)
                throw new InvalidOperationException("У организации уже есть активная подписка.");
        }
        private decimal CalculateItem(
            Dictionary<string, SubscriptionItem> dict,
            string name,
            decimal quantity)
                {
                    if (!dict.TryGetValue(name, out var config))
                        throw new Exception($"Элемент '{name}' не найден");

                    if (quantity < config.MinQuantity || quantity > config.MaxQuantity)
                        throw new Exception(
                            $"Количество для '{name}' должно быть от {config.MinQuantity} до {config.MaxQuantity}");

                    if (config.Step > 0 && (quantity - config.MinQuantity) % config.Step != 0)
                        throw new Exception(
                            $"Количество для '{name}' должно изменяться с шагом {config.Step}");

                    return quantity * config.PricePerUnit;
        }
        private async Task<decimal> GetCostSub(CreateCustomSubDto model, CancellationToken ct)
        {
            var configs = await _conetxet.SubscriptionItems
                .AsNoTracking()
                .ToListAsync(ct);

            if (!configs.Any())
                throw new Exception("Конфигурация подписки не найдена");

            var dict = configs.ToDictionary(x => x.Name);

            decimal totalPerMonth = 0;
            if (dict.TryGetValue("Базовая лицензия", out var baseLicense))
            {
                totalPerMonth += baseLicense.PricePerUnit;
            }
            else
            {
                throw new Exception("Базовая лицензия не найдена");
            }
            totalPerMonth += CalculateItem(dict, "Участники", model.MebmersCount);
            totalPerMonth += CalculateItem(dict, "Команды", model.TeamsCount);
            totalPerMonth += CalculateItem(dict, "Проекты", model.ProjectsCount);

            if (model.MonthCount <= 0)
                throw new Exception("Количество месяцев должно быть больше 0");

            return totalPerMonth * model.MonthCount;
        }

        public async Task<List<SubscriptionItemDto>> GetConfigAsync(CancellationToken ct)
        {
            return await _conetxet.SubscriptionItems
                .Select(x => new SubscriptionItemDto
                {
                    Name = x.Name,
                    Unit = x.Unit,
                    PricePerUnit = x.PricePerUnit,
                    MinQuantity = x.MinQuantity,
                    MaxQuantity = x.MaxQuantity,
                    Step = x.Step,
                    Description = x.Description
                }).ToListAsync()
                    ?? throw new Exception("Не найдено");
                }


        public async Task<long> CreateCustomSubAsync(CreateCustomSubDto model, CancellationToken ct)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();
            var now = DateTime.UtcNow;


            var isSub = await _conetxet.Subscriptions.AnyAsync(s => s.OrganizationId == orgId && s.StatusId != (int)SubStatus.Canceled);
            if (isSub)
            {
                throw new InvalidOperationException("У вас есть активная подписка");
            }

            var activeStatusId = await GetStatusIdByCodeAsync("active", ct);
            await CheckActiveSubscribtionAsync(orgId, activeStatusId, ct);

            await using var tx = await _conetxet.Database.BeginTransactionAsync(ct);

            var totalPrice = await GetCostSub(model, ct);

            var sub = new Subscription
            {
                OrganizationId = orgId,
                StatusId = activeStatusId,
                StartDate = now,
                EndDate = now.AddMonths(model.MonthCount),
                CreatedAt = now,

                MaxMembers = model.MebmersCount,
                MaxTeams = model.TeamsCount,
                MaxProjects = model.ProjectsCount,

                Currency = "RUB",
                PriceCents = (int)(totalPrice * 100)
            };

            _conetxet.Subscriptions.Add(sub);
            await _conetxet.SaveChangesAsync(ct);

            var payment = new Payment
            {
                OrganizationId = orgId,
                SubscriptionId = sub.Id,

                AmountCents = (int)(totalPrice * 100),
                Currency = "RUB",

                Provider = PaymentConstants.ProviderInternal,
                Status = PaymentConstants.StatusPaid,
                Reason = PaymentConstants.ReasonSubscribe,
                CreatedAt = now,
                PaidAtUtc = now
            };

            _conetxet.Payments.Add(payment);
            await _conetxet.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            await _receiptPdf.GenerateAndStoreAsync(payment.Id);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                Action = "create",
                EntityType = "subscription",
                EntityId = sub.Id,
                Title = "Создана кастомная подписка",
                NewValues = new
                {
                    sub.StartDate,
                    sub.EndDate,
                    sub.MaxMembers,
                    sub.MaxTeams,
                    sub.MaxProjects,
                    PaymentId = payment.Id
                }
            }, ct);

            return payment.Id;
        }

        public async Task<long> SubscribeToPlanAsync(BySubDto model, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();
            var now = DateTime.UtcNow;

            var isSub = await _conetxet.Subscriptions.AnyAsync(s => s.OrganizationId == orgId && s.StatusId != (int)SubStatus.Canceled);
            if(isSub)
            {
                throw new InvalidOperationException("У вас есть активная подписка");
            }

            await EnsureOwnerAsync(orgId, userId, ct);

            var plan = await _conetxet.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == model.PlanId, ct);

            if (plan == null)
                throw new KeyNotFoundException("Тарифный план не найден.");

            var activeStatusId = await GetStatusIdByCodeAsync("active", ct);
            await CheckActiveSubscribtionAsync(orgId, activeStatusId, ct);

            await using var tx = await _conetxet.Database.BeginTransactionAsync(ct);

            var sub = new Subscription
            {
                OrganizationId = orgId,
                PlanId = plan.Id,
                StatusId = activeStatusId,
                StartDate = now,
                EndDate = now.AddMonths(model.MonthCount),
                CreatedAt = now
            };
            ApplyPlanSnapshotToSubscription(sub, plan);

            _conetxet.Subscriptions.Add(sub);
            await _conetxet.SaveChangesAsync(ct);

            var payment = new Payment
            {
                OrganizationId = orgId,
                SubscriptionId = sub.Id,
                AmountCents = plan.BasePriceCents * model.MonthCount,
                Currency = plan.Currency,
                Provider = PaymentConstants.ProviderInternal,
                Status = PaymentConstants.StatusPaid,
                Reason = PaymentConstants.ReasonSubscribe,
                CreatedAt = now,
                PaidAtUtc = now
            };

            _conetxet.Payments.Add(payment);
            await _conetxet.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            await _receiptPdf.GenerateAndStoreAsync(payment.Id);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                Action = "create",
                EntityType = "subscription",
                EntityId = sub.Id,
                EntityName = plan.Name,
                Title = "Оформлена подписка",
                NewValues = new
                {
                    sub.PlanId,
                    sub.StartDate,
                    sub.EndDate,
                    sub.PriceCents,
                    PaymentId = payment.Id
                }
            }, ct);

            return payment.Id;
        }


        public async Task<SubCheckInfoDto> CheckSubAsync(CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var sub = await _conetxet.Subscriptions
                .FirstOrDefaultAsync(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == (int)SubStatus.Active &&
                    s.EndDate != null &&
                    s.EndDate > DateTime.UtcNow,
                    ct);

            if (sub == null)
            {
                return new SubCheckInfoDto { Status = SubStatus.None };
            }
            return new SubCheckInfoDto { Status = SubStatus.Active, EndDate = sub.EndDate };
        }


        public async Task<long> ExtendActiveSubscriptionAsync(int monthCount, CancellationToken ct = default)
        {
            if (monthCount <= 0)
                throw new InvalidOperationException("monthCount должен быть > 0.");

            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();
            var now = DateTime.UtcNow;

            await EnsureOwnerAsync(orgId, userId, ct);

            var activeStatusId = await GetStatusIdByCodeAsync("active", ct);

            var sub = await _conetxet.Subscriptions
                .FirstOrDefaultAsync(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == activeStatusId &&
                    s.EndDate != null &&
                    s.EndDate > now,
                    ct);

            if (sub == null)
                throw new InvalidOperationException("Активная подписка не найдена.");

            await using var tx = await _conetxet.Database.BeginTransactionAsync(ct);

            sub.EndDate = sub.EndDate!.Value.AddMonths(monthCount);
            sub.UpdatedAt = now;

            var payment = new Payment
            {
                OrganizationId = orgId,
                SubscriptionId = sub.Id,
                AmountCents = sub.PriceCents * monthCount,
                Currency = sub.Currency,
                Provider = PaymentConstants.ProviderInternal,
                Status = PaymentConstants.StatusPaid,
                Reason = PaymentConstants.ReasonExtend,
                CreatedAt = now,
                PaidAtUtc = now
            };

            _conetxet.Payments.Add(payment);
            await _conetxet.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            await _receiptPdf.GenerateAndStoreAsync(payment.Id);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                Action = "update",
                EntityType = "subscription",
                EntityId = sub.Id,
                Title = "Подписка продлена",
                NewValues = new
                {
                    sub.EndDate,
                    MonthCount = monthCount,
                    PaymentId = payment.Id
                }
            }, ct);

            return payment.Id;
        }


        public async Task CancelActiveSubscriptionAsync(CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();
            var now = DateTime.UtcNow;

            await EnsureOwnerAsync(orgId, userId, ct);

            var activeStatusId = await GetStatusIdByCodeAsync("active", ct);
            var canceledStatusId = await GetStatusIdByCodeAsync("canceled", ct);

            var sub = await _conetxet.Subscriptions
                .FirstOrDefaultAsync(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == activeStatusId &&
                    s.EndDate != null &&
                    s.EndDate > now,
                    ct);

            if (sub == null)
                throw new InvalidOperationException("Активная подписка не найдена.");

            sub.StatusId = canceledStatusId;
            sub.EndDate = now;
            sub.UpdatedAt = now;

            await _conetxet.SaveChangesAsync(ct);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                Action = "update",
                EntityType = "subscription",
                EntityId = sub.Id,
                Title = "Подписка отменена",
                NewValues = new { sub.StatusId, sub.EndDate }
            }, ct);
        }
        private static int? CalcLeft(int max, int current)
        {
            if (max <= 0) return null;
            var left = max - current;
            return left < 0 ? 0 : left;
        }

        private static bool CanAddOne(int max, int current)
        {
            if (max <= 0) return true; 
            return current + 1 <= max;
        }

        public async Task<CurrentSubscriptionDto> GetCurrentAsync(CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var now = DateTime.UtcNow;

            var activeStatusId = await _conetxet.SubscriptionStatuses
                .AsNoTracking()
                .Where(s => s.Code == "active")
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (activeStatusId == 0)
                return new CurrentSubscriptionDto { HasActive = false, StatusCode = null, IsExpired = true };

            var sub = await _conetxet.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .Where(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == activeStatusId &&
                    s.EndDate != null &&
                    s.EndDate > now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(ct);

            if (sub == null)
            {
                return new CurrentSubscriptionDto
                {
                    HasActive = false,
                    IsExpired = true
                };
            }

            var membersCount = await _conetxet.Members
                .AsNoTracking()
                .CountAsync(m => m.OrganizationId == orgId, ct);

            var teamsCount = await _conetxet.ProjectTeams
                .AsNoTracking()
                .CountAsync(t => t.Project.OrganizationId == orgId && t.IsActive != false, ct);

            var projectsCount = await _conetxet.Projects
                .AsNoTracking()
                .CountAsync(p => p.OrganizationId == orgId && p.IsArchived != true, ct);
            var end = sub.EndDate;
            var daysLeft = end.HasValue ? (int)Math.Floor((end.Value - now).TotalDays) : (int?)null;

            var membersLeft = CalcLeft(sub.MaxMembers, membersCount);
            var teamsLeft = CalcLeft(sub.MaxTeams, teamsCount);
            var projectsLeft = CalcLeft(sub.MaxProjects, projectsCount);

            return new CurrentSubscriptionDto
            {
                HasActive = true,

                SubscriptionId = sub.Id,
                PlanId = sub.PlanId,
                PlanName = sub.Plan?.Name,
                StatusCode = sub.Status?.Code,

                StartDateUtc = sub.StartDate,
                EndDateUtc = sub.EndDate,
                DaysLeft = daysLeft,
                IsExpired = sub.EndDate.HasValue && sub.EndDate.Value <= now,

                Currency = sub.Currency,
                PriceCentsPerMonth = sub.PriceCents,

                MaxMembers = sub.MaxMembers,
                MaxTeams = sub.MaxTeams,
                MaxProjects = sub.MaxProjects,

                MembersCount = membersCount,
                TeamsCount = teamsCount,
                ProjectsCount = projectsCount,

                MembersLeft = membersLeft,
                TeamsLeft = teamsLeft,
                ProjectsLeft = projectsLeft,

                AllowInvites = sub.AllowInvites,
                AllowNewTeams = sub.AllowNewTeams,
                AllowNewProjects = sub.AllowNewProjects,

                CanInviteMember = sub.AllowInvites && CanAddOne(sub.MaxMembers, membersCount),
                CanCreateTeam = sub.AllowNewTeams && CanAddOne(sub.MaxTeams, teamsCount),
                CanCreateProject = sub.AllowNewProjects && CanAddOne(sub.MaxProjects, projectsCount),
            };
        }
    }
}
