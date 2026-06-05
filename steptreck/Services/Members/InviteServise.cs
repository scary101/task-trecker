using Microsoft.EntityFrameworkCore;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Email;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.InviteDTO;
using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.Enums;
using System.Data;

namespace steptreck.API.Services.Members
{
    public class InviteServise
    {
        private const string UserAlreadyInOrganizationMessage = "Пользователь уже состоит в организации";

        private readonly AppDbContext _context;
        private readonly UserHelper _userhelper;
        private readonly InvationTokenHelper _invationtokenhelper;
        private readonly IConfiguration _config;
        private readonly EmailHelper _emailHelper;
        private readonly TokenHelper _tokenHelper;
        private readonly ISubscriptionGate _gate;
        private readonly AuditService _auditService;

        public InviteServise(AppDbContext appDbContext, UserHelper userHelper, InvationTokenHelper invationTokenHelper, IConfiguration config, EmailHelper emailHelper, TokenHelper tokenHelper, ISubscriptionGate gate, AuditService auditService)
        {
            _context = appDbContext;
            _userhelper = userHelper;
            _invationtokenhelper = invationTokenHelper;
            _config = config;
            _emailHelper = emailHelper;
            _tokenHelper = tokenHelper;
            _gate = gate;
            _auditService = auditService;
        }



        public async Task SendInvite(RegisterMebmerDto model, CancellationToken ct = default)
        {
            if (model.RoleId == (int)RoleEnum.Owner)
                throw new InvalidOperationException("Нельзя назначить роль");

            var orgId = _userhelper.GetCurrentOrganizationId();
            var currentUserId = _userhelper.GetCurrentUserId();
            var email = model.CorporateEmail.Trim().ToLower();

            await _gate.ThrowIfCannotInviteMemberAsync(orgId, ct);

            var invitedUserId = await _context.Users
                .Where(u => u.CorporateEmail.ToLower() == email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (invitedUserId.HasValue)
            {
                var userHasOrganization = await _context.Members
                    .AnyAsync(m => m.UserId == invitedUserId.Value && m.IsActive, ct);

                if (userHasOrganization)
                    throw new InvalidOperationException(UserAlreadyInOrganizationMessage);
            }

            var activeInvite = await _context.Invitations.AnyAsync(i =>
                i.OrganizationId == orgId &&
                i.CorporateEmail == email &&
                i.ExpiresAt > DateTime.UtcNow, ct);

            if (activeInvite)
                throw new InvalidOperationException("Инвайт уже отправлен и ещё активен");

            var rawToken = _invationtokenhelper.GenerateRawToken();
            var tokenHash = _invationtokenhelper.HashToken(rawToken);
            var expiresHours = int.Parse(_config["Invitation:ExpiresHours"] ?? "72");

            var invitation = new Invitation
            {
                OrganizationId = orgId,
                CorporateEmail = email,
                RoleId = model.RoleId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expiresHours)
            };
            _context.Invitations.Add(invitation);

            await _context.SaveChangesAsync(ct);


            var orgName = await _context.Organizations
                .Where(o => o.Id == orgId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(ct) ?? "—";

            var owner = await _context.Members.FirstOrDefaultAsync(m =>
                m.UserId == currentUserId && m.OrganizationId == orgId, ct);

            if (owner == null)
                throw new InvalidOperationException("Не найден отправитель (member) в этой организации");

            await _auditService.LogWithAllIdAsync(orgId, owner.Id, new AuditLogCreateDto
            {
                Action = "create",
                EntityType = "invitation",
                EntityId = invitation.Id,
                EntityName = email,
                Title = "Отправлено приглашение",
                NewValues = new
                {
                    invitation.CorporateEmail,
                    invitation.RoleId,
                    invitation.ExpiresAt
                }
            }, ct);

            var inviteInfo = new SendInviteDto
            {
                OrgName = orgName,
                FullNameSender = _userhelper.GetFullNameMember(owner)
            };

            var link = $"{_config["App:FrontendUrl"]}/invite/accept?token={Uri.EscapeDataString(rawToken)}";

            await _emailHelper.SendInvite(email, link, inviteInfo);
        }

        public async Task<AcceptInviteResult> AcceptInviteAsync(
            string token,
            int currentUserId,
            CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync(new object[] { currentUserId }, ct);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            var tokenHash = _invationtokenhelper.HashToken(token);

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var invite = await _context.Invitations
                .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

            if (invite == null)
                return new AcceptInviteResult
                {
                    Success = false,
                    Message = "Приглашение не найдено"
                };

            if (invite.UsedAt != null)
                return new AcceptInviteResult
                {
                    Success = false,
                    Message = "Приглашение уже использовано"
                };

            if (invite.ExpiresAt <= DateTime.UtcNow)
                return new AcceptInviteResult
                {
                    Success = false,
                    Message = "Срок действия приглашения истёк"
                };

            if (!string.Equals(invite.CorporateEmail, user.CorporateEmail, StringComparison.OrdinalIgnoreCase))
                return new AcceptInviteResult
                {
                    Success = false,
                    Message = "Приглашение предназначено для другого email"
                };

            var userHasOtherOrganization = await _context.Members
                .AnyAsync(m =>
                    m.UserId == currentUserId &&
                    m.OrganizationId != invite.OrganizationId &&
                    m.IsActive, ct);

            if (userHasOtherOrganization)
                return new AcceptInviteResult
                {
                    Success = false,
                    Message = UserAlreadyInOrganizationMessage
                };

            var existingMember = await _context.Members
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m =>
                    m.OrganizationId == invite.OrganizationId &&
                    m.UserId == currentUserId, ct);

            if (existingMember != null)
            {
                invite.UsedAt = DateTime.UtcNow;
                invite.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await _auditService.LogWithAllIdAsync(invite.OrganizationId, existingMember.Id, new AuditLogCreateDto
                {
                    TargetMemberId = existingMember.Id,
                    Action = "update",
                    EntityType = "invitation",
                    EntityId = invite.Id,
                    EntityName = invite.CorporateEmail,
                    Title = "Приглашение отмечено использованным",
                    NewValues = new { invite.UsedAt, invite.UpdatedAt }
                }, ct);

                var orgToken = _tokenHelper.GenerateOrgToken(user, existingMember);

                return new AcceptInviteResult
                {
                    Success = true,
                    Message = "Вы уже состоите в организации",
                    OrganizationId = invite.OrganizationId,
                    AccessToken = orgToken
                };
            }

            await _gate.ThrowIfCannotInviteMemberAsync(invite.OrganizationId, ct);

            var member = new Member
            {
                UserId = currentUserId,
                Patronymic = user.Patronymic,
                Name = user.Name,
                Surname = user.Surname,
                RoleId = invite.RoleId,
                IsActive = true,
                OrganizationId = invite.OrganizationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Members.Add(member);

            invite.UsedAt = DateTime.UtcNow;
            invite.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogWithAllIdAsync(invite.OrganizationId, member.Id, new AuditLogCreateDto
            {
                TargetMemberId = member.Id,
                Action = "create",
                EntityType = "member",
                EntityId = member.Id,
                EntityName = $"{member.Surname} {member.Name}".Trim(),
                Title = "Принято приглашение",
                NewValues = new
                {
                    invite.Id,
                    invite.CorporateEmail,
                    member.RoleId
                }
            }, ct);

            await _context.Entry(member)
                .Reference(m => m.Role)
                .LoadAsync(ct);

            var newToken = _tokenHelper.GenerateOrgToken(user, member);

            return new AcceptInviteResult
            {
                Success = true,
                Message = "Приглашение принято",
                OrganizationId = invite.OrganizationId,
                AccessToken = newToken
            };
        }

    }
}
