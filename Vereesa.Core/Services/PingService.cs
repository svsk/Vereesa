using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Vereesa.Core.Services
{
    public class PingService
    {
        private DiscordSocketClient _discord;

        public PingService(DiscordSocketClient discord)
        {
            _discord = discord;
            _discord.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (message.Content == "!ping") 
            {
                var responseMessage = await message.Channel.SendMessageAsync($"Pong!");
                
                var responseTimestamp = responseMessage.Timestamp.ToUnixTimeMilliseconds();
                var messageSentTimestamp = message.Timestamp.ToUnixTimeMilliseconds();
                await responseMessage.ModifyAsync((msg) => { msg.Content = $"Pong! (Responded after {responseTimestamp - messageSentTimestamp} ms)"; });
            }
        }
    }
}