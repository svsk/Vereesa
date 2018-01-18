using System.Linq;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions
{
    public static class SocketMessageExtensions
    {
        public static string GetCommand(this SocketMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
                return null;

            return message.Content.Split(' ').First();
        }

        public static string[] GetCommandArgs(this SocketMessage message)
        {
            return message.Content?.Split(' ')?.Skip(1)?.ToArray();
        }
    }
}