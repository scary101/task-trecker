using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.Constants;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ChatDTOs;
using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.Enums;
using System;

namespace steptreck.API.Services.Chat
{
    public class ChatService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;

        public ChatService(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }
        private async Task<bool> IsOrganizationOwner(int memberId, int orgId, CancellationToken ct)
        {
            return await _context.Members
                .AnyAsync(m =>
                    m.Id == memberId &&
                    m.OrganizationId == orgId &&
                    m.Role.Id == (int)RoleEnum.Owner, ct);
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

        public async Task EnsureCanAccessTeam(int teamId, int memberId, int orgId, CancellationToken ct = default)
        {
            var teamOrgId = await _context.ProjectTeams
                .Where(t => t.Id == teamId && t.IsActive && !t.Project.IsArchived)
                .Select(t => t.Project.OrganizationId)
                .FirstOrDefaultAsync(ct);

            if (teamOrgId == 0 || teamOrgId != orgId)
                throw new InvalidOperationException("Команда не найдена или не принадлежит организации.");

            var isOwner = await IsOrganizationOwner(memberId, orgId, ct);

            if (isOwner)
                return;

            var isInTeam = await _context.ProjectTeamMembers
                .AnyAsync(x =>
                    x.TeamId == teamId &&
                    x.MemberId == memberId &&
                    x.Team.IsActive &&
                    !x.Team.Project.IsArchived, ct);

            if (!isInTeam)
                throw new UnauthorizedAccessException("У вас нет доступа к этому чату.");
        }

        public async Task<ChatMessageDto> CreateMessage(
            int teamId,
            int memberId,
            string text,
            long? replyToMessageId,
            CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Пустое сообщение.");

            text = text.Trim();

            if (text.Length > ChatConstants.MaxMessageTextLength)
                throw new InvalidOperationException($"Сообщение слишком длинное. Максимум {ChatConstants.MaxMessageTextLength} символов.");

            await EnsureCanAccessTeam(teamId, memberId, orgId, ct);

            TeamChatMessage? replyMessage = null;

            if (replyToMessageId.HasValue)
            {
                replyMessage = await _context.Set<TeamChatMessage>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == replyToMessageId.Value &&
                        x.OrganizationId == orgId &&
                        x.TeamId == teamId,
                        ct);

                if (replyMessage == null)
                    throw new InvalidOperationException("Сообщение для ответа не найдено.");
            }

            var entity = new TeamChatMessage
            {
                OrganizationId = orgId,
                TeamId = teamId,
                SenderMemberId = memberId,
                Text = text,
                ReplyToMessageId = replyToMessageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(entity);
            await _context.SaveChangesAsync(ct);

            var senderIds = new List<int> { memberId };

            if (replyMessage != null)
                senderIds.Add(replyMessage.SenderMemberId);

            var senders = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && senderIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Surname, m.Name, m.Patronymic, m.RoleId })
                .ToDictionaryAsync(x => x.Id, ct);

            string BuildName(int id)
            {
                if (!senders.TryGetValue(id, out var sender))
                    return "";

                return string.Join(" ",
                    new[] { sender.Surname, sender.Name, sender.Patronymic }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            return new ChatMessageDto
            {
                Id = entity.Id,
                TeamId = entity.TeamId,
                SenderMemberId = entity.SenderMemberId,
                SenderName = BuildName(entity.SenderMemberId),
                Text = entity.Text,
                Role = senders.TryGetValue(entity.SenderMemberId, out var currentSender) ? currentSender.RoleId : 0,
                CreatedAt = entity.CreatedAt,
                ReplyToMessageId = entity.ReplyToMessageId,
                ReplyTo = replyMessage == null
                    ? null
                    : new ChatReplyDto
                    {
                        Id = replyMessage.Id,
                        Text = replyMessage.Text,
                        SenderName = BuildName(replyMessage.SenderMemberId)
                    }
            };
        }

        public async Task<List<ChatMessageDto>> GetLastMessages(
            int teamId,
            long? beforeId,
            int take,
            CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int memberId = await GetCurrentMemberId(ct);

            await EnsureCanAccessTeam(teamId, memberId, orgId, ct);

            take = Math.Clamp(take, 1, 100);

            var q = _context.Set<TeamChatMessage>()
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.TeamId == teamId);

            if (beforeId.HasValue)
                q = q.Where(m => m.Id < beforeId.Value);

            var rows = await q
                .OrderByDescending(m => m.Id)
                .Take(take)
                .ToListAsync(ct);

            var messageIds = rows.Select(x => x.Id).ToList();

            var replyIds = rows
                .Where(x => x.ReplyToMessageId.HasValue)
                .Select(x => x.ReplyToMessageId!.Value)
                .Distinct()
                .ToList();

            var replyMessages = await _context.Set<TeamChatMessage>()
                .AsNoTracking()
                .Where(x =>
                    x.OrganizationId == orgId &&
                    x.TeamId == teamId &&
                    replyIds.Contains(x.Id))
                .ToListAsync(ct);

            var senderIds = rows
                .Select(r => r.SenderMemberId)
                .Concat(replyMessages.Select(r => r.SenderMemberId))
                .Distinct()
                .ToList();

            var senders = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && senderIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Surname, m.Name, m.Patronymic, m.RoleId })
                .ToDictionaryAsync(x => x.Id, ct);

            var reactions = await _context.Set<TeamChatMessageReaction>()
                .AsNoTracking()
                .Where(x => messageIds.Contains(x.MessageId))
                .GroupBy(x => new { x.MessageId, x.Emoji })
                .Select(g => new
                {
                    g.Key.MessageId,
                    g.Key.Emoji,
                    Count = g.Count(),
                    ReactedByMe = g.Any(x => x.MemberId == memberId)
                })
                .ToListAsync(ct);

            var reactionsByMessage = reactions
                .GroupBy(x => x.MessageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ChatReactionDto
                    {
                        Emoji = x.Emoji,
                        Count = x.Count,
                        ReactedByMe = x.ReactedByMe
                    }).ToList()
                );

            var replyMessagesById = replyMessages.ToDictionary(x => x.Id);

            string BuildFullName(int senderMemberId)
            {
                if (!senders.TryGetValue(senderMemberId, out var s))
                    return "";

                return string.Join(" ",
                    new[] { s.Surname, s.Name, s.Patronymic }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            var dtos = rows.Select(r =>
            {
                reactionsByMessage.TryGetValue(r.Id, out var messageReactions);

                TeamChatMessage? replyMessage = null;

                if (r.ReplyToMessageId.HasValue)
                {
                    replyMessagesById.TryGetValue(
                        r.ReplyToMessageId.Value,
                        out replyMessage
                    );
                }

                return new ChatMessageDto
                {
                    Id = r.Id,
                    TeamId = r.TeamId,
                    SenderMemberId = r.SenderMemberId,
                    SenderName = BuildFullName(r.SenderMemberId),
                    Text = r.Text,
                    CreatedAt = r.CreatedAt,
                    Role = senders.TryGetValue(r.SenderMemberId, out var sender) ? sender.RoleId : 0,

                    ReplyToMessageId = r.ReplyToMessageId,
                    ReplyTo = replyMessage == null
                        ? null
                        : new ChatReplyDto
                        {
                            Id = replyMessage.Id,
                            Text = replyMessage.Text,
                            SenderName = BuildFullName(replyMessage.SenderMemberId)
                        },

                    IsPinned = r.IsPinned,
                    PinnedAt = r.PinnedAt,
                    PinnedByMemberId = r.PinnedByMemberId,
                    Reactions = messageReactions ?? new List<ChatReactionDto>()
                };
            }).ToList();

            return dtos;
        }
        public async Task<List<TeamMemberUsernameDto>> GetTeamUsernamesAsync(
            int teamId,
            CancellationToken ct)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int memberId = await GetCurrentMemberId(ct);

            await EnsureCanAccessTeam(teamId, memberId, orgId, ct);

            return await _context.ProjectTeamMembers
                .AsNoTracking()
                .Where(x =>
                    x.TeamId == teamId &&
                    x.Team.IsActive &&
                    !x.Team.Project.IsArchived)
                .Select(x => new TeamMemberUsernameDto
                {
                    MemberId = x.Member.Id,
                    Username = x.Member.Username ?? ""
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .OrderBy(x => x.Username)
                .ToListAsync(ct);
        }
        public async Task<ChatReactionChangedDto> ToggleReaction(
            long messageId,
            int memberId,
            string emoji,
            CancellationToken ct = default)
        {
            emoji = emoji.Trim();

            if (string.IsNullOrWhiteSpace(emoji))
                throw new Exception("Emoji is empty.");

            if (emoji.Length > 32)
                throw new Exception("Emoji is too long.");

            var message = await _context.TeamChatMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == messageId, ct);

            if (message == null)
                throw new Exception("Сообщение не найдено.");

            int orgId = _userHelper.GetCurrentOrganizationId();
            await EnsureCanAccessTeam(message.TeamId, memberId, orgId, ct);

            var existing = await _context.TeamChatMessageReactions
                .FirstOrDefaultAsync(x =>
                    x.MessageId == messageId &&
                    x.MemberId == memberId &&
                    x.Emoji == emoji,
                    ct);

            var isAdded = existing == null;

            if (existing == null)
            {
                _context.TeamChatMessageReactions.Add(new TeamChatMessageReaction
                {
                    MessageId = messageId,
                    MemberId = memberId,
                    Emoji = emoji,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _context.TeamChatMessageReactions.Remove(existing);
            }

            await _context.SaveChangesAsync(ct);

            var count = await _context.TeamChatMessageReactions
                .CountAsync(x =>
                    x.MessageId == messageId &&
                    x.Emoji == emoji,
                    ct);

            return new ChatReactionChangedDto
            {
                MessageId = messageId,
                TeamId = message.TeamId,
                Emoji = emoji,
                Count = count,
                IsAdded = isAdded,
                MemberId = memberId
            };
        }
        public async Task<ChatPinChangedDto> TogglePinMessage(
            long messageId,
            int memberId,
            CancellationToken ct = default)
        {
            var message = await _context.Set<TeamChatMessage>()
                .FirstOrDefaultAsync(x => x.Id == messageId, ct);

            if (message == null)
                throw new Exception("Сообщение не найдено.");

            int orgId = _userHelper.GetCurrentOrganizationId();

            await EnsureCanAccessTeam(message.TeamId, memberId, orgId, ct);

            message.IsPinned = !message.IsPinned;

            if (message.IsPinned)
            {
                message.PinnedAt = DateTime.UtcNow;
                message.PinnedByMemberId = memberId;
            }
            else
            {
                message.PinnedAt = null;
                message.PinnedByMemberId = null;
            }

            await _context.SaveChangesAsync(ct);
            await _auditService.LogWithAllIdAsync(orgId, memberId, new AuditLogCreateDto
            {
                TeamId = message.TeamId,
                Action = "update",
                EntityType = "team_chat_message",
                EntityId = message.Id,
                EntityName = message.Text.Length > 80 ? message.Text[..80] : message.Text,
                Title = message.IsPinned ? "Сообщение закреплено" : "Сообщение откреплено",
                NewValues = new
                {
                    message.IsPinned,
                    message.PinnedAt,
                    message.PinnedByMemberId
                }
            }, ct);

            return new ChatPinChangedDto
            {
                MessageId = message.Id,
                TeamId = message.TeamId,
                IsPinned = message.IsPinned,
                PinnedAt = message.PinnedAt,
                PinnedByMemberId = message.PinnedByMemberId
            };
        }
        public async Task<List<ChatListItemDto>> GetChatsAsync(CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int memberId = await GetCurrentMemberId(ct);

            var isOwner = await _context.Members
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Id == memberId &&
                    m.OrganizationId == orgId &&
                    m.Role.Id == (int)RoleEnum.Owner,
                    ct);

            IQueryable<ProjectTeam> teamsQuery;

            if (isOwner)
            {
                teamsQuery = _context.ProjectTeams
                    .AsNoTracking()
                    .Where(t =>
                        t.Project.OrganizationId == orgId &&
                        t.IsActive &&
                        !t.Project.IsArchived);
            }
            else
            {
                teamsQuery = _context.ProjectTeamMembers
                    .AsNoTracking()
                    .Where(x =>
                        x.MemberId == memberId &&
                        x.Team.IsActive &&
                        !x.Team.Project.IsArchived)
                    .Select(x => x.Team);
            }

            var teams = await teamsQuery
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.ProjectId,
                    ProjectName = t.Project.Name
                })
                .ToListAsync(ct);

            var teamIds = teams.Select(t => t.Id).ToList();

            var lastMessages = await _context.TeamChatMessages
                .AsNoTracking()
                .Where(m =>
                    m.OrganizationId == orgId &&
                    teamIds.Contains(m.TeamId))
                .GroupBy(m => m.TeamId)
                .Select(g => g
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.TeamId,
                        x.Text,
                        x.CreatedAt
                    })
                    .FirstOrDefault())
                .ToListAsync(ct);

            var lastMessagesDict = lastMessages
                .Where(x => x != null)
                .ToDictionary(x => x!.TeamId, x => x!);

            var result = teams.Select(t =>
            {
                lastMessagesDict.TryGetValue(t.Id, out var last);

                return new ChatListItemDto
                {
                    TeamId = t.Id,
                    ProjectId = t.ProjectId,
                    TeamName = t.Name,
                    ProjectName = t.ProjectName,

                    LastMessageText = last?.Text,
                    LastMessageAt = last?.CreatedAt,

                    UnreadCount = 0 // пока заглушка
                };
            })
            .OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue)
            .ToList();

            return result;
        }
    }
}
