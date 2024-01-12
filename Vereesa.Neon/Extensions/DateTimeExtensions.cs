using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace Vereesa.Neon.Extensions
{
    public class TzHelper
    {
        public static DateTimeZone ServerTz => DateTimeZoneProviders.Tzdb["Europe/Paris"];
    }

    public static class DateTimeExtensions
    {
        public static DateTimeOffset ToCentralEuropeanTime(this DateTimeOffset dateTimeOffset)
        {
            return new ZonedDateTime(
                Instant.FromDateTimeOffset(dateTimeOffset),
                DateTimeZoneProviders.Tzdb["Europe/Paris"]
            ).ToDateTimeOffset();
        }

        public static DateTimeOffset NowInCentralEuropeanTime()
        {
            var zonedDateTime = new ZonedDateTime(
                SystemClock.Instance.GetCurrentInstant(),
                DateTimeZoneProviders.Tzdb["Europe/Paris"]
            );
            return zonedDateTime.ToDateTimeOffset();
        }

        public static DateTime ToUtc(this DateTime dateTime, string timestampTimeZone)
        {
            var localDateTime = LocalDateTime.FromDateTime(dateTime);
            var orignatingTimeZone = DateTimeZoneProviders.Tzdb[timestampTimeZone]; //The "DB" (aka the Google Sheet) providing the values is set up with Europe/Berlin time...
            var zonedDateTime = orignatingTimeZone.AtStrictly(localDateTime);

            return zonedDateTime.ToDateTimeUtc();
        }

        public static ZonedDateTime AsServerTime(this Instant instant)
        {
            return new ZonedDateTime(instant, DateTimeZoneProviders.Tzdb["Europe/Paris"]);
        }

        public static ZonedDateTime ParseToZonedDateTime(
            this string stringTimestamp,
            string format,
            string ianaTimeZone
        )
        {
            var pattern = LocalDateTimePattern.CreateWithInvariantCulture(format);
            var localDateTime = pattern.Parse(stringTimestamp).Value;
            return localDateTime.InZoneLeniently(DateTimeZoneProviders.Tzdb[ianaTimeZone]);
        }

        public static string ToPrettyTime(this ZonedDateTime dateTime)
        {
            return dateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        public static string ToPrettyDuration(this Duration duration)
        {
            if (duration > Duration.FromHours(1))
            {
                return duration.ToString("-H 'hours and' mm 'minutes'", CultureInfo.InvariantCulture);
            }
            else if (duration > Duration.FromMinutes(1))
            {
                return duration.ToString("m 'minutes and' ss 'seconds'", CultureInfo.InvariantCulture);
            }
            else
            {
                return duration.ToString("s 'seconds'", CultureInfo.InvariantCulture);
            }
        }
    }
}
