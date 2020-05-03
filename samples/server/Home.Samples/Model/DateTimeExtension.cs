using System;

namespace Lucky.Home.Model
{
    static class DateTimeExtension
    {
        public static string ToIso(this DateTime? start)
        {
            if (start.HasValue)
            {
                // Use milliseconds version (3 frames) instead of 6 frames obtained using the standard 'o' format.
                // This will allow compatibility with JS
                return start.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffzzz");
            }
            else
            {
                return null;
            }
        }

        public static DateTime? FromIso(this string str)
        {
            if (str != null)
            {
                return DateTime.ParseExact(str, "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz", null);
            }
            else
            {
                return null;
            }
        }

    }
}
