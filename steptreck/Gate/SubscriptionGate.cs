using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;

namespace steptreck.API.Gate
{
    public interface ISubscriptionGate
    {
        Task<bool> HasActiveAsync(int orgId, CancellationToken ct);

        Task<(bool ok, string? error)> CanInviteMemberAsync(int orgId, CancellationToken ct);
        Task<(bool ok, string? error)> CanCreateTeamAsync(int orgId, CancellationToken ct);
        Task<(bool ok, string? error)> CanCreateProjectAsync(int orgId, CancellationToken ct);

        Task ThrowIfCannotInviteMemberAsync(int orgId, CancellationToken ct);
        Task ThrowIfCannotCreateTeamAsync(int orgId, CancellationToken ct);
        Task ThrowIfCannotCreateProjectAsync(int orgId, CancellationToken ct);

        Task ThrowIfOverLimitsAsync(int orgId, CancellationToken ct); // общий “санити-чек”
    }

    public class SubscriptionGate : ISubscriptionGate
    {
        private readonly AppDbContext _db;

        public SubscriptionGate(AppDbContext db)
        {
            _db = db;
        }

        private async Task<int> GetStatusIdByCodeAsync(string code, CancellationToken ct)
        {
            var id = await _db.SubscriptionStatuses
                .AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (id == 0)
                throw new InvalidOperationException($"Статус подписки '{code}' не найден.");

            return id;
        }

        private async Task<Subscription?> GetActiveSubscriptionAsync(int orgId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var activeId = await GetStatusIdByCodeAsync("active", ct);

            return await _db.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.OrganizationId == orgId &&
                    s.StatusId == activeId &&
                    s.EndDate != null &&
                    s.EndDate > now)
                .OrderByDescending(s => s.EndDate) 
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasActiveAsync(int orgId, CancellationToken ct)
            => await GetActiveSubscriptionAsync(orgId, ct) != null;


        private Task<int> CountMembersAsync(int orgId, CancellationToken ct)
            => _db.Members.AsNoTracking()
                .CountAsync(m => m.OrganizationId == orgId, ct);

        private Task<int> CountTeamsAsync(int orgId, CancellationToken ct)
            => _db.ProjectTeams.AsNoTracking()
                .CountAsync(t => t.Project.OrganizationId == orgId, ct);

        private Task<int> CountProjectsAsync(int orgId, CancellationToken ct)
            => _db.Projects.AsNoTracking()
                .CountAsync(p => p.OrganizationId == orgId && p.IsArchived != true, ct);
        private static bool IsLimitExceeded(int current, int max)
            => max > 0 && current > max;

        private static bool WouldExceedOnAdd(int current, int max)
            => max > 0 && current + 1 > max;


        public async Task<(bool ok, string? error)> CanInviteMemberAsync(int orgId, CancellationToken ct)
        {
            var sub = await GetActiveSubscriptionAsync(orgId, ct);
            if (sub == null)
                return (false, "Нет активной подписки.");

            if (!sub.AllowInvites)
                return (false, "Тариф не позволяет приглашать участников.");

            var members = await CountMembersAsync(orgId, ct);
            if (WouldExceedOnAdd(members, sub.MaxMembers))
                return (false, $"Достигнут лимит участников: {members}/{sub.MaxMembers}.");

            return (true, null);
        }

        public async Task<(bool ok, string? error)> CanCreateTeamAsync(int orgId, CancellationToken ct)
        {
            var sub = await GetActiveSubscriptionAsync(orgId, ct);
            if (sub == null)
                return (false, "Нет активной подписки.");

            if (!sub.AllowNewTeams)
                return (false, "Тариф не позволяет создавать команды.");

            var teams = await CountTeamsAsync(orgId, ct);
            if (WouldExceedOnAdd(teams, sub.MaxTeams))
                return (false, $"Достигнут лимит команд: {teams}/{sub.MaxTeams}.");

            return (true, null);
        }

        public async Task<(bool ok, string? error)> CanCreateProjectAsync(int orgId, CancellationToken ct)
        {
            var sub = await GetActiveSubscriptionAsync(orgId, ct);
            if (sub == null)
                return (false, "Нет активной подписки.");

            if (!sub.AllowNewProjects)
                return (false, "Тариф не позволяет создавать проекты.");

            var projects = await CountProjectsAsync(orgId, ct);
            if (WouldExceedOnAdd(projects, sub.MaxProjects))
                return (false, $"Достигнут лимит проектов: {projects}/{sub.MaxProjects}.");

            return (true, null);
        }

        public async Task ThrowIfCannotInviteMemberAsync(int orgId, CancellationToken ct)
        {
            var (ok, error) = await CanInviteMemberAsync(orgId, ct);
            if (!ok) throw new InvalidOperationException(error);
        }

        public async Task ThrowIfCannotCreateTeamAsync(int orgId, CancellationToken ct)
        {
            var (ok, error) = await CanCreateTeamAsync(orgId, ct);
            if (!ok) throw new InvalidOperationException(error);
        }

        public async Task ThrowIfCannotCreateProjectAsync(int orgId, CancellationToken ct)
        {
            var (ok, error) = await CanCreateProjectAsync(orgId, ct);
            if (!ok) throw new InvalidOperationException(error);
        }

        /// <summary>
        /// Полезно дергать при логине/при открытии рабочего пространства:
        /// если подписка активна, но данные уже "вылезли" за лимиты (из-за изменения тарифа),
        /// можно показать баннер/заблокировать создание нового.
        /// </summary>
        public async Task ThrowIfOverLimitsAsync(int orgId, CancellationToken ct)
        {
            var sub = await GetActiveSubscriptionAsync(orgId, ct);
            if (sub == null)
                throw new InvalidOperationException("Нет активной подписки.");

            var members = await CountMembersAsync(orgId, ct);
            var teams = await CountTeamsAsync(orgId, ct);
            var projects = await CountProjectsAsync(orgId, ct);

            if (IsLimitExceeded(members, sub.MaxMembers))
                throw new InvalidOperationException($"Превышен лимит участников: {members}/{sub.MaxMembers}.");

            if (IsLimitExceeded(teams, sub.MaxTeams))
                throw new InvalidOperationException($"Превышен лимит команд: {teams}/{sub.MaxTeams}.");

            if (IsLimitExceeded(projects, sub.MaxProjects))
                throw new InvalidOperationException($"Превышен лимит проектов: {projects}/{sub.MaxProjects}.");
        }
    }
}
