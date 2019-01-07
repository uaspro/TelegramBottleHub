using System;

namespace TelegramBottleHub.General.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeSpan DefaultDateTimeOffset = TimeSpan.FromHours(2);

        public static DateTime GetNow()
        {
            return DateTimeOffset.Now.ToOffset(DefaultDateTimeOffset).DateTime;
        }
    }
}
