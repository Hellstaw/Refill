using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp2
{
    public partial class Window1 : Window
    {
        private FuelManager fuelManager;

        public Window1()
        {
            InitializeComponent();
            fuelManager = new FuelManager();
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void AddingFuell(object sender, RoutedEventArgs e)
        {
            // Проверяем, что выбрано тип топлива
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
                // Добавляем топливо (сообщение показывается внутри FuelManager)
                fuelManager.AddFuel(selectedFuelType, quantity);

                // Закрываем окно после успешного добавления
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении топлива: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }
    }
}