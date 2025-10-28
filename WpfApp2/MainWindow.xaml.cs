using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private connectionBD dbService;
        private FuelManager fuelManager;

        public MainWindow()
        {
            InitializeComponent();
            dbService = new connectionBD();
            fuelManager = new FuelManager();

            var app = (App)Application.Current;
            this.Title = $"Главное окно - {app.CurrentUsername}";
            LoadData();
            InitializePumpDisplays();
        }

        private void InitializePumpDisplays()
        {
            try
            {
                PetrolPump1.Text = "0";
                PetrolPump2.Text = "0";
                PetrolPump3.Text = "0";
                PetrolPump4.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации колонок: {ex.Message}");
            }
        }

        // Простой метод отсчета
        public async Task StartSimpleCountdown(int pumpNumber, double quantity)
        {
            TextBox targetPump = GetPumpTextBox(pumpNumber);
            if (targetPump != null)
            {
                double current = quantity;

                // Меняем цвет на время отсчета (опционально)
                targetPump.Background = System.Windows.Media.Brushes.LightYellow;

                // Отсчет от quantity до 0
                while (current > 0)
                {
                    targetPump.Text = current.ToString("0.0");
                    current -= 0.1; // Уменьшаем на 0.1 каждый шаг
                    if (current < 0) current = 0;

                    // Ждем 50 мс между обновлениями
                    await Task.Delay(50);
                }

                targetPump.Text = "0";

                // Возвращаем обычный цвет
                targetPump.Background = System.Windows.Media.Brushes.White;
            }
        }

        private TextBox GetPumpTextBox(int pumpNumber)
        {
            return pumpNumber switch
            {
                1 => PetrolPump1,
                2 => PetrolPump2,
                3 => PetrolPump3,
                4 => PetrolPump4,
                _ => null
            };
        }

        // Остальные методы без изменений...
        private void LoadData()
        {
            try
            {
                DataTable data = dbService.GetOperationHistory();
                UsersDataGrid.ItemsSource = data.DefaultView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window1 taskWindow = new Window1();
            taskWindow.Show();
        }

        private void ViewSessionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable sessionHistory = dbService.GetSessionHistory();

                Window historyWindow = new Window
                {
                    Title = "История сессий",
                    Width = 800,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                DataGrid dataGrid = new DataGrid
                {
                    ItemsSource = sessionHistory.DefaultView,
                    AutoGenerateColumns = true
                };

                historyWindow.Content = dataGrid;
                historyWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            deletefuell deleteWindow = new deletefuell(this);
            deleteWindow.Show();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();

            RefreshButton.Content = "✓ Обновлено";

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) =>
            {
                RefreshButton.Content = "🔄 Обновить";
                timer.Stop();
            };
            timer.Start();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                                        "Подтверждение выхода",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Записываем время выхода
                var app = (App)Application.Current;
                if (app.CurrentWorkerId > 0)
                {
                    dbService.LogLogout(app.CurrentWorkerId, app.CurrentLoginId);
                }

                // Закрываем приложение

                LoginReg loginReg   = new LoginReg();
                loginReg.Show();
                this.Close();
                //Application.Current.Shutdown();
            }
        }
    }
}