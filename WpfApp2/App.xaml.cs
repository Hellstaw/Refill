using System.Windows;

namespace WpfApp2
{
    public partial class App : Application
    {
        // Свойства для хранения информации о текущем пользователе
        public int CurrentWorkerId { get; set; }
        public int CurrentLoginId { get; set; }
        public string CurrentUsername { get; set; } = string.Empty;

        private connectionBD _connectionBD;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _connectionBD = new connectionBD();
            this.SessionEnding += App_SessionEnding;
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (CurrentWorkerId > 0)
            {
                _connectionBD.LogLogout(CurrentWorkerId, CurrentLoginId);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (CurrentWorkerId > 0)
            {
                _connectionBD.LogLogout(CurrentWorkerId, CurrentLoginId);
            }
            base.OnExit(e);
        }
    }
}