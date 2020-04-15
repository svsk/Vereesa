using System.Collections.Generic;
using System.Threading.Tasks;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Core.Interfaces;

namespace Vereesa.Core.Services
{
    public class MessageService
    {
        private IDiscordSocketClient _discord;
        private object _checkInterval;
        private IMessageProvider _messageProvider;

        public MessageService(IDiscordSocketClient discord, IMessageProvider messageProvider)
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