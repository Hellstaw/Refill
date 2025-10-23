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
    /// <summary>
    /// Логика взаимодействия для deletefuell.xaml
    /// </summary>
    public partial class deletefuell : Window
    {
        private FuelManager fuelManager;
        public deletefuell()
        {
            InitializeComponent();
            fuelManager = new FuelManager();
        }

        private void AddingFuell(object sender, RoutedEventArgs e)
        {// Проверяем, что выбрано тип топлива
            if (InputComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип топлива!",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что введено корректное количество
            if (!double.TryParse(InputTextBox.Text, out double quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество топлива!",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Получаем выбранный тип топлива
            string selectedFuelType = (InputComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            try
            {
                double currentQuantity = fuelManager.GetFuelQuantity(selectedFuelType);
                fuelManager.SubtractFuel(selectedFuelType, quantity);
                double newQuantity = fuelManager.GetFuelQuantity(selectedFuelType);

                MessageBox.Show($"Топливо успешно списано!\n" +
                              $"Тип: {selectedFuelType}\n" +
                              $"Списано: {quantity}\n" +
                              $"Было: {currentQuantity}\n" +
                              $"Стало: {newQuantity}",
                              "Успех",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при списании топлива: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
