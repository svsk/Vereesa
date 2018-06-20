using System;

namespace Vereesa.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToCentralEuropeanTime (this DateTime dateTime) 
        {
            var cetTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
            return cetTime;
        }
    }
}