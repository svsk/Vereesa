using System;
using NodaTime;

namespace Vereesa.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset ToCentralEuropeanTime (this DateTimeOffset dateTimeOffset) 
        {
            return new ZonedDateTime(Instant.FromDateTimeOffset(dateTimeOffset), DateTimeZoneProviders.Tzdb["Europe/Paris"]).ToDateTimeOffset();
        }

        public static DateTimeOffset NowInCentralEuropeanTime () 
        {
            var zonedDateTime = new ZonedDateTime(SystemClock.Instance.GetCurrentInstant(), DateTimeZoneProviders.Tzdb["Europe/Paris"]);
            return zonedDateTime.ToDateTimeOffset();
        }

        public static DateTime ToUtc(this DateTime dateTime, string timestampTimeZone)
        {            
            var localDateTime = LocalDateTime.FromDateTime(dateTime);
            var orignatingTimeZone = DateTimeZoneProviders.Tzdb[timestampTimeZone]; //The "DB" (aka the Google Sheet) providing the values is set up with Europe/Berlin time...
            var zonedDateTime = orignatingTimeZone.AtStrictly(localDateTime);

            return zonedDateTime.ToDateTimeUtc();
        }
    }
}