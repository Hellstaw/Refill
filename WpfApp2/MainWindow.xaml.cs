using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private connectionBD dbService;

        public MainWindow()
        {
            InitializeComponent();
            dbService = new connectionBD();
            var app = (App)Application.Current;
            this.Title = $"Главное окно - {app.CurrentUsername}";
            LoadData();
        }
        private void LoadData()
        {
            try
            {
                DataTable data = dbService.GetFuel();
                UsersDataGrid.ItemsSource = data.DefaultView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window1 taskWindow = new Window1();
            taskWindow.Show();
        }
        //переход на окно который я убрал пока что
        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    bd bdd = new bd();
        //    bdd.Show();
     
        //}


        //lkz кнопки выхода
        //protected override void OnClosed(EventArgs e)
        //{
        //    var app = (App)Application.Current;
        //    if (app.CurrentWorkerId > 0)
        //    {
        //        dbService.LogLogout(app.CurrentWorkerId, app.CurrentLoginId);
        //    }
        //    base.OnClosed(e);
        //}

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
            deletefuell deletefuell = new deletefuell();
            deletefuell.Show();
        }
    }
}