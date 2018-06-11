using System;
using System.Linq;

namespace Vereesa.Core.Extensions
{
    public static class StringExtensions
    {
        public static ulong? ToChannelId(this string channelRef)
        {
            ulong.TryParse(channelRef.Replace("<#", string.Empty).Replace(">", string.Empty), out var channelId);
            return channelId;
        }

        public static string GetCommand(this string rawMessage)
        {
            return rawMessage.Split(' ').First();
        }

        public static string[] Split(this string rawString, string separator) 
        {
            return rawString.Split(new string[] { separator }, StringSplitOptions.None);
        }
    }
}