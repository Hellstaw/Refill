using System;
using System.Collections.Generic;
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
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }
        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public void AddingFuell(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(InputTextBox.Text,out int number))
            {
                int x = Convert.ToInt32(InputTextBox.Text);
                MessageBox.Show("!!!!!!!"+x);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное число!",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }
    }
}
