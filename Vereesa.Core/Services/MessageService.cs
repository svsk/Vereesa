using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Helpers;
using Vereesa.Core.Interfaces;

namespace Vereesa.Core.Services
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
            _checkInterval = await TimerHelpers.SetTimeoutAsync(async () => { await CheckForNewMessagesAsync(); }, 60 * 1000 * 3, true, true);
        }

        private async Task CheckForNewMessagesAsync()
        {
            IEnumerable<IProvidedMessage> newMessages = await _messageProvider.CheckForNewMessagesAsync();
        }
    }
}