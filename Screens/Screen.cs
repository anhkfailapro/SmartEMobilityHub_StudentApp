using System.Windows;
using SmartEMobilityHub.Ui;

namespace SmartEMobilityHub.Screens
{
    public abstract class Screen
    {
        protected static AppShell Shell => DemoApp.Shell;

        public virtual string? Title => null;
        public virtual bool ShowBack => true;
        public virtual NavTab? Tab => null;
        public virtual bool WantsTick => false;

        public abstract UIElement BuildContent();

        public virtual UIElement? BuildActions() => null;

        public virtual void OnBack() => Shell.Back();

        public virtual void OnTick()
        {
        }
    }
}
