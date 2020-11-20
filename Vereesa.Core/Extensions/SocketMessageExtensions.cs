using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions
{
	public static class SocketMessageExtensions
	{
		public static string GetCommand(this SocketMessage message)
			=> ((IMessage)message).GetCommand();

		public static string[] GetCommandArgs(this SocketMessage message)
			=> ((IMessage)message).GetCommandArgs();
	}

	public static class IMessageExtensions
	{
		public static string GetCommand(this IMessage message)
		{
			if (string.IsNullOrEmpty(message.Content))
				return null;

			return message.Content.Split(' ').First();
		}

		public static string[] GetCommandArgs(this IMessage message)
		{
			return message.Content?.Split(' ')?.Skip(1)?.ToArray();
		}
	}
}