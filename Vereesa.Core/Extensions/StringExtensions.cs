using System;
using System.Collections.Generic;
using System.Globalization;
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

		public static string StripTrim(this string inputString)
		{
			inputString = inputString.Replace(Environment.NewLine, string.Empty);
			inputString = inputString.Replace("\n", string.Empty);
			inputString = inputString.Replace("\r", string.Empty);
			inputString = string.Join(' ', inputString.Split(' ').Where(slug => slug != ""));
			inputString = inputString.Trim();

			return inputString;
		}

		public static bool IsNullOrWhitespace(this string inputString) => string.IsNullOrWhiteSpace(inputString);

		public static NumberFormatInfo GetThousandSeparatorFormat()
		{
			var numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
			numberFormat.NumberGroupSeparator = " ";
			return numberFormat;
		}

		public static string ToTitleCase(this string inputString)
		{
			return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(inputString);
		}

		public static string Join(this IEnumerable<string> values, string separator)
		{
			return string.Join(separator, values);
		}

		public static string Splice(this string rawString, int spliceStart, string spliceString, int spliceEnd)
		{
			return rawString.Substring(0, spliceStart) + spliceString + rawString.Substring(spliceEnd);
		}

		public static int IndexOfAfter(this string haystack, string needle, string itemThatMustOccurBefore)
		{
			var beforeItemIndex = haystack.IndexOf(itemThatMustOccurBefore);
			return haystack.Substring(beforeItemIndex).IndexOf(needle) + beforeItemIndex;
		}
	}
}