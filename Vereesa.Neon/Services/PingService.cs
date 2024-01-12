using System.ComponentModel;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Neon.Services
{
    public class PingService : IBotService
    {
        [OnCommand("!ping")]
        [Description("Ping Vereesa to check if she is still alive and accepting commands.")]
        public async Task HandleMessageAsync(IMessage message)
        {
            var responseMessage = await message.Channel.SendMessageAsync($"Pong!");
            var responseTimestamp = responseMessage.Timestamp.ToUnixTimeMilliseconds();
            var messageSentTimestamp = message.Timestamp.ToUnixTimeMilliseconds();
            await responseMessage.ModifyAsync(
                (msg) =>
                {
                    msg.Content = $"Pong! (Responded after {responseTimestamp - messageSentTimestamp} ms)";
                }
            );
        }

        [OnCommand("!ip")]
        [Authorize("Guild Master")]
        [Description("Ask Vereesa what IP she's running under. Restricted to Veinlash.")]
        public async Task GetIp(IMessage message)
        {
            var client = new RestClient();
            var response = await client.ExecuteAsync(new RestRequest("https://ip.seeip.org/", Method.GET));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                response = await client.ExecuteAsync(new RestRequest("https://api.ipify.org/?format=text", Method.GET));
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _ = await message.Channel.SendMessageAsync($"No IP provider gave a proper response.");
            }
            else
            {
                _ = await message.Channel.SendMessageAsync($"{response.Content}");
            }
        }
    }
}
