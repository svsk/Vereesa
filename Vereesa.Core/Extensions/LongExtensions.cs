using System;

namespace Vereesa.Core.Extensions
{
	public static class LongExtensions
	{
		public static string ToHoursMinutesSeconds(this long unixTicks)
		{
			var t = TimeSpan.FromSeconds(unixTicks);

			return string.Format("{0:D2}:{1:D2}:{2:D2}",
				t.Hours,
				t.Minutes,
				t.Seconds);
		}

		public static string ToDaysHoursMinutesSeconds(this long unixTicks)
		{
			var t = TimeSpan.FromSeconds(unixTicks);

			return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}",
				t.Days,
				t.Hours,
				t.Minutes,
				t.Seconds);
		}
	}
}
