namespace SmartEMobilityHub.Screens
{
    public static class TimeOptions
    {
        public static readonly string[] Days = { "Hôm nay", "Ngày mai" };

        /// <summary>Các mốc 30 phút để chọn giờ, tính từ giờ demo <paramref name="now"/>.</summary>
        public static List<DateTime> Slots(DateTime now, int dayOffset, int count = 8)
        {
            DateTime start;
            if (dayOffset == 0)
            {
                var b = now.AddMinutes(10);
                start = b.Date.AddHours(b.Hour).AddMinutes(b.Minute < 30 ? 30 : 60);
            }
            else
            {
                start = now.Date.AddDays(dayOffset).AddHours(8);
            }
            return Enumerable.Range(0, count).Select(i => start.AddMinutes(30 * i)).ToList();
        }
    }
}
