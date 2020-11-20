using Discord;

namespace Vereesa.Core.Extensions
{
	public static class ULongExtensions
	{
		public static string MentionPerson(this ulong id) => MentionUtils.MentionUser(id);

		public static string MentionRole(this ulong id) => MentionUtils.MentionRole(id);

		public static string MentionChannel(this ulong id) => MentionUtils.MentionChannel(id);
	}
}