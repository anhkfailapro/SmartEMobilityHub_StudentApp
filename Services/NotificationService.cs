using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class NotificationService
    {
        private readonly StudentContext Ctx;

        public NotificationService(StudentContext ctx) => Ctx = ctx;

        public void Send(string title, string body, Tone tone) =>
            Ctx.Store.Notifications.Add(new AppNotification { Title = title, Body = body, Time = Ctx.Clock.Now, Tone = tone });

        public List<AppNotification> Newest() => Ctx.Store.Notifications.OrderByDescending(n => n.Time).ToList();

        public int UnreadCount => Ctx.Store.Notifications.Count(n => !n.Read);

        public void MarkAllRead()
        {
            foreach (var n in Ctx.Store.Notifications) n.Read = true;
        }
    }
}
