using System;
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
	}
}