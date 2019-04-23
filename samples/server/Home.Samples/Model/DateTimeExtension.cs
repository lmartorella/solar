using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Model
{
    static class DateTimeExtension
    {
        public static string ToIso(this DateTime? start)
        {
            if (start.HasValue)
            {
                return start.Value.ToString("o");
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
                return DateTime.ParseExact(str, "o", null);
            }
            else
            {
                return null;
            }
        }

    }
}
