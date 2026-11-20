using SmartEMobilityHub.Models;

namespace SmartEMobilityHub.Services
{
    public class NotificationService
    {
        public void Send(string title, string body, Tone tone) =>
            DemoApp.Store.Notifications.Add(new AppNotification { Title = title, Body = body, Time = DemoApp.Clock.Now, Tone = tone });

        public List<AppNotification> Newest() => DemoApp.Store.Notifications.OrderByDescending(n => n.Time).ToList();

        public int UnreadCount => DemoApp.Store.Notifications.Count(n => !n.Read);

        public void MarkAllRead()
        {
            foreach (var n in DemoApp.Store.Notifications) n.Read = true;
        }
    }
}
