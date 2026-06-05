using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ChatDTOs;

namespace steptreck.Web.Services
{
    public class ChatClient : IAsyncDisposable
    {
        private readonly NavigationManager _nav;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;

        public HubConnection? Connection { get; private set; }

        public ChatClient(NavigationManager nav, IJwtService jwt, IConfiguration config)
        {
            _nav = nav;
            _jwt = jwt;
            _config = config;
        }

        public async Task StartAsync()
        {
            if (Connection is not null && Connection.State != HubConnectionState.Disconnected)
                return;

            var apiBaseUrl = _config["Api:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                apiBaseUrl = _nav.BaseUri;

            var hubUrl = new Uri(new Uri(apiBaseUrl), "/hubs/chat").ToString();

            Connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () => await _jwt.GetTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            await Connection.StartAsync();
        }

        public IDisposable? OnNewMessage(Action<ChatMessageDto> handler)
        {
            if (Connection is null)
                return null;

            return Connection.On<ChatMessageDto>("ChatNewMessage", handler);
        }

        public IDisposable? OnReactionChanged(Action<ChatReactionChangedDto> handler)
        {
            if (Connection is null)
                return null;

            return Connection.On<ChatReactionChangedDto>("ChatReactionChanged", handler);
        }

        public IDisposable? OnPinChanged(Action<ChatPinChangedDto> handler)
        {
            if (Connection is null)
                return null;

            return Connection.On<ChatPinChangedDto>("ChatPinChanged", handler);
        }
        public IDisposable? OnNotify(Action<NotificationDto> handler)
        {
            if (Connection is null)
                return null;

            return Connection.On<NotificationDto>("notify:new", handler);
        }


        public async Task SendMessage(int teamId, string text, long? replyToMessageId = null)
        {
            if (Connection is null)
                throw new InvalidOperationException("SignalR Connection is null");

            if (replyToMessageId.HasValue)
            {
                await Connection.InvokeAsync(
                    "SendReplyMessage",
                    teamId,
                    replyToMessageId.Value,
                    text
                );

                return;
            }

            await Connection.InvokeAsync(
                "SendMessage",
                teamId,
                text
            );
        }

        public async Task ToggleReaction(long messageId, string emoji)
        {
            if (Connection is null)
                throw new InvalidOperationException("SignalR Connection is null");

            await Connection.InvokeAsync("ToggleReaction", messageId, emoji);
        }

        public async Task TogglePinMessage(long messageId)
        {
            if (Connection is null)
                throw new InvalidOperationException("SignalR Connection is null");

            await Connection.InvokeAsync("TogglePinMessage", messageId);
        }


        public async ValueTask DisposeAsync()
        {
            if (Connection is not null)
                await Connection.DisposeAsync();
        }
    }
}
