namespace SmartEMobilityHub.Screens
{
    public static class TimeOptions
    {
        public static readonly string[] Days = { "Hôm nay", "Ngày mai" };

        public static List<DateTime> Slots(int dayOffset, int count = 8)
        {
            var now = DemoApp.Clock.Now;
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
