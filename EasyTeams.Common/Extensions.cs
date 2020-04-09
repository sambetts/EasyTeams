using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;

namespace EasyTeams.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Is this just a Date (no time)? Assume if it's midnight exactly, then no.
        /// </summary>
        public static bool HasValidTime(this DateTime dt)
        {
            if (dt.Hour == 0 && dt.Minute == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Does this timex include a time?
        /// </summary>
        public static bool HasValidHoursAndMinutesTime(this TimexProperty timex)
        {
            if (timex.Hour.HasValue && timex.Minute.HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Does this timex include a date?
        /// </summary>
        public static bool HasValidDate(this TimexProperty timex)
        {
            if (timex.Year.HasValue && timex.Month.HasValue && timex.DayOfMonth.HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static DateTime GetDateTime(this TimexProperty timex)
        {
            if (timex.HasValidDate())
            {
                DateTime dt = new DateTime(timex.Year.Value, timex.Month.Value, timex.DayOfMonth.Value);
                if (timex.HasValidHoursAndMinutesTime())
                {
                    dt = dt.AddHours(timex.Hour.Value);
                    dt = dt.AddMinutes(timex.Minute.Value);
                }

                return dt;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timex), "Timex is ambigious");
            }
        }

    }
}
