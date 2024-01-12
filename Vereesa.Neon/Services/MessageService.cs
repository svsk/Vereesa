using Discord.WebSocket;
using Vereesa.Neon.Helpers;
using Vereesa.Neon.Interfaces;

namespace Vereesa.Neon.Services
{
    public class MessageService
    {
        private DiscordSocketClient _discord;
        private object _checkInterval;
        private IMessageProvider _messageProvider;

        public MessageService(DiscordSocketClient discord, IMessageProvider messageProvider)
        {
            _discord = discord;
            _discord.Ready -= InitializeServiceAsync;
            _discord.Ready += InitializeServiceAsync;
            _messageProvider = messageProvider;
        }

        private async Task InitializeServiceAsync()
        {
            _checkInterval = await TimerHelpers.SetTimeoutAsync(
                async () =>
                {
                    await CheckForNewMessagesAsync();
                },
                60 * 1000 * 3,
                true,
                true
            );
        }

        private async Task CheckForNewMessagesAsync()
        {
            IEnumerable<IProvidedMessage> newMessages = await _messageProvider.CheckForNewMessagesAsync();
        }
    }
}
