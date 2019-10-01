using System;
using NodaTime;

namespace Vereesa.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToCentralEuropeanTime (this DateTime dateTime) 
        {
            var cetTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
            return cetTime;
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