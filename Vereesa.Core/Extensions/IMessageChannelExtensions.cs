using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace Vereesa.Core.Extensions
{
	public static class IMessageChannelExtensions
	{
		public static async Task<Func<Task>> SendDynamicMessage(this IMessageChannel channel,
			Func<string> dynamicMessage)
		{
			var messageToUpdate = await channel.SendMessageAsync(dynamicMessage());
			return new Func<Task>(async () =>
			{
				await messageToUpdate.ModifyAsync((msg) =>
				{
					msg.Content = dynamicMessage();
				});
			});
		}

		public static async Task<Func<string, Task>> SendDynamicMessage(this IMessageChannel channel, string initialMessage)
		{
			var messageToUpdate = await channel.SendMessageAsync(initialMessage);
			return new Func<string, Task>(async (newMessage) =>
			{
				await messageToUpdate.ModifyAsync((msg) =>
				{
					msg.Content = newMessage;
				});
			});
		}

		public static async Task<List<IUserMessage>> SendLongMessageAsync(this IMessageChannel channel, string message)
		{
			var messages = new List<IUserMessage>();

			if (message.Length < 2000)
			{
				messages.Add(await channel.SendMessageAsync(message));
				return messages;
			}

			var words = message.Split(" ");
			var currentMessage = "";
			foreach (var word in words)
			{
				if ((currentMessage + " " + word).Length > 2000)
				{
					messages.Add(await channel.SendMessageAsync(currentMessage));
					currentMessage = "";
				}
				else
				{
					currentMessage += (" " + word);
				}
			}

			return messages;
		}
	}
}