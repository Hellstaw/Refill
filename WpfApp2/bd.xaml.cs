using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp2
{
    public partial class bd : Window
    {
        private connectionBD dbService;
        public bd()
        {
            InitializeComponent();
            dbService = new connectionBD();
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
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable data = dbService.GetSessionHistory();
                UsersDataGrid.ItemsSource = data.DefaultView;
                //MessageBox.Show($"Загружено {data.Rows.Count} записей", "Успех");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }
    }
}
