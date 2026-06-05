using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Chat
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly ChatService _chat;
        private readonly NotificationsService _notifications;

        public ChatHub(
            AppDbContext context,
            UserHelper userHelper,
            ChatService chat,
            NotificationsService notifications)
        {
            _context = context;
            _userHelper = userHelper;
            _chat = chat;
            _notifications = notifications;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine(
                $"[ChatHub] Connected. " +
                $"ConnectionId={Context.ConnectionId}, " +
                $"Auth={Context.User?.Identity?.IsAuthenticated}"
            );

            try
            {
                int orgId = _userHelper.GetCurrentOrganizationId();

                int memberId = await _chat.GetCurrentMemberId(
                    Context.ConnectionAborted
                );

                Console.WriteLine(
                    $"[ChatHub] User resolved. " +
                    $"OrgId={orgId}, MemberId={memberId}"
                );

                var isOwner = await _context.Members
                    .AsNoTracking()
                    .AnyAsync(
                        m =>
                            m.Id == memberId &&
                            m.OrganizationId == orgId &&
                            m.Role.Id == (int)RoleEnum.Owner,
                        Context.ConnectionAborted
                    );

                List<int> teamIds;

                if (isOwner)
                {
                    teamIds = await _context.ProjectTeams
                        .AsNoTracking()
                        .Where(
                            t =>
                                t.Project.OrganizationId == orgId &&
                                t.IsActive &&
                                !t.Project.IsArchived
                        )
                        .Select(t => t.Id)
                        .ToListAsync(Context.ConnectionAborted);
                }
                else
                {
                    teamIds = await _context.ProjectTeamMembers
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.MemberId == memberId &&
                                x.Team.IsActive &&
                                !x.Team.Project.IsArchived
                        )
                        .Select(x => x.TeamId)
                        .ToListAsync(Context.ConnectionAborted);
                }

                Console.WriteLine(
                    $"[ChatHub] IsOwner={isOwner}. " +
                    $"Teams: {string.Join(",", teamIds)}"
                );

                foreach (var teamId in teamIds)
                {
                    var teamGroup = $"org:{orgId}:team:{teamId}";

                    await Groups.AddToGroupAsync(
                        Context.ConnectionId,
                        teamGroup,
                        Context.ConnectionAborted
                    );

                    Console.WriteLine(
                        $"[ChatHub] Joined group: {teamGroup}"
                    );
                }

                var memberGroup = $"member:{memberId}";

                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    memberGroup,
                    Context.ConnectionAborted
                );

                Console.WriteLine(
                    $"[ChatHub] Joined group: {memberGroup}"
                );

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== OnConnectedAsync ERROR =========="
                );

                Console.WriteLine(ex);

                Console.WriteLine(
                    "============================================"
                );

                throw;
            }
        }

        public override Task OnDisconnectedAsync(
            Exception? exception)
        {
            Console.WriteLine(
                $"[ChatHub] Disconnected. " +
                $"ConnectionId={Context.ConnectionId}"
            );

            if (exception is not null)
            {
                Console.WriteLine(exception);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(
            int teamId,
            string text)
        {
            await SendMessageInternal(
                teamId,
                text,
                null
            );
        }

        public async Task SendReplyMessage(
            int teamId,
            long replyToMessageId,
            string text)
        {
            await SendMessageInternal(
                teamId,
                text,
                replyToMessageId
            );
        }

        private async Task SendMessageInternal(
            int teamId,
            string text,
            long? replyToMessageId)
        {
            Console.WriteLine(
                $"[ChatHub] SendMessageInternal called. " +
                $"TeamId={teamId}, " +
                $"TextLen={text?.Length ?? 0}"
            );

            try
            {
                int orgId =
                    _userHelper.GetCurrentOrganizationId();

                int memberId =
                    await _chat.GetCurrentMemberId();

                text ??= string.Empty;

                var dto = await _chat.CreateMessage(
                    teamId,
                    memberId,
                    text,
                    replyToMessageId,
                    Context.ConnectionAborted
                );

                await _notifications.NotifyChatMentionsAsync(
                    teamId,
                    memberId,
                    dto.SenderName,
                    dto.Text,
                    Context.ConnectionAborted
                );

                await Clients
                    .Group($"org:{orgId}:team:{teamId}")
                    .SendAsync(
                        "ChatNewMessage",
                        dto,
                        Context.ConnectionAborted
                    );

                Console.WriteLine(
                    "[ChatHub] Message sent successfully"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== SendMessage ERROR =========="
                );

                Console.WriteLine(ex);

                Console.WriteLine(
                    "======================================="
                );

                throw new HubException(ex.Message);
            }
        }

        public Task<string> Ping()
        {
            Console.WriteLine("[ChatHub] Ping called");
            return Task.FromResult("pong");
        }

        public async Task ToggleReaction(
            long messageId,
            string emoji)
        {
            Console.WriteLine(
                $"[ChatHub] ToggleReaction called. " +
                $"MessageId={messageId}, Emoji={emoji}"
            );

            try
            {
                int orgId =
                    _userHelper.GetCurrentOrganizationId();

                int memberId =
                    await _chat.GetCurrentMemberId();

                var dto = await _chat.ToggleReaction(
                    messageId,
                    memberId,
                    emoji
                );

                var group =
                    $"org:{orgId}:team:{dto.TeamId}";

                await Clients.Group(group)
                    .SendAsync(
                        "ChatReactionChanged",
                        dto,
                        Context.ConnectionAborted
                    );

                Console.WriteLine(
                    $"[ChatHub] Sent ChatReactionChanged to {group}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== ToggleReaction ERROR =========="
                );

                Console.WriteLine(ex);

                Console.WriteLine(
                    "=========================================="
                );

                throw new HubException(ex.Message);
            }
        }

        public async Task TogglePinMessage(long messageId)
        {
            Console.WriteLine(
                $"[ChatHub] TogglePinMessage called. " +
                $"MessageId={messageId}"
            );

            try
            {
                int orgId =
                    _userHelper.GetCurrentOrganizationId();

                int memberId =
                    await _chat.GetCurrentMemberId();

                var dto = await _chat.TogglePinMessage(
                    messageId,
                    memberId
                );

                var group =
                    $"org:{orgId}:team:{dto.TeamId}";

                await Clients.Group(group)
                    .SendAsync(
                        "ChatPinChanged",
                        dto,
                        Context.ConnectionAborted
                    );

                Console.WriteLine(
                    $"[ChatHub] Sent ChatPinChanged to {group}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== TogglePinMessage ERROR =========="
                );

                Console.WriteLine(ex);

                Console.WriteLine(
                    "============================================"
                );

                throw new HubException(ex.Message);
            }
        }
    }
}