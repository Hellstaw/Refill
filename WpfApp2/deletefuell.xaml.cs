using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace WpfApp2
{
    public partial class deletefuell : Window
    {
        private FuelManager fuelManager;
        private connectionBD dbService;
        private MainWindow mainWindow;

        // Статический словарь для отслеживания занятых колонок
        private static Dictionary<int, bool> busyPumps = new Dictionary<int, bool>();

        public class KolonkaItem
        {
            public int Id { get; set; }
            public string KolonkaNumber { get; set; }
            public string FuelType { get; set; }

            public override string ToString()
            {
                return $"{KolonkaNumber} ({FuelType})";
            }
        }

        public deletefuell(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            fuelManager = new FuelManager();
            dbService = new connectionBD();
            LoadKolonki();
        }

        private void LoadKolonki()
        {
            try
            {
                var kolonki = fuelManager.GetAvailableKolonki();

                // Фильтруем колонки - убираем занятые
                var availableKolonki = new List<KolonkaItem>();
                foreach (var kolonka in kolonki)
                {
                    int pumpNumber = GetPumpNumberFromKolonka(kolonka);
                    if (!IsPumpBusy(pumpNumber))
                    {
                        availableKolonki.Add(kolonka);
                    }
                }

                KolonkaComboBox.ItemsSource = availableKolonki;
                if (availableKolonki.Count > 0)
                    KolonkaComboBox.SelectedIndex = 0;
                else
                    MessageBox.Show("Все колонки заняты! Подождите завершения текущих заправок.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки колонок: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void RefuelButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что выбрана колонка
            if (KolonkaComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите колонку!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что выбран тип топлива
            if (FuelTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип топлива!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что выбран тип оплаты
            if (PaymentTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип оплаты!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем количество
            if (!double.TryParse(QuantityTextBox.Text, out double quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество литров!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем выбранные значения
            var selectedKolonka = (KolonkaItem)KolonkaComboBox.SelectedItem;
            string selectedFuelType = (FuelTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string paymentType = (PaymentTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            try
            {
                // Получаем номер колонки
                int pumpNumber = GetPumpNumberFromKolonka(selectedKolonka);

                // Проверяем, не занята ли колонка
                if (IsPumpBusy(pumpNumber))
                {
                    MessageBox.Show($"Колонка {pumpNumber} уже занята! Подождите завершения текущей заправки.",
                                  "Колонка занята", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Помечаем колонку как занятую
                SetPumpBusy(pumpNumber, true);

                // Отключаем кнопку на время заправки
                RefuelButton.IsEnabled = false;
                RefuelButton.Content = "Идет заправка...";

                // ЗАКРЫВАЕМ ОКНО СРАЗУ ПОСЛЕ НАЖАТИЯ КНОПКИ
                this.Close();

                // Запускаем отсчет в основном окне
                if (mainWindow != null)
                {
                    await mainWindow.StartSimpleCountdown(pumpNumber, quantity);
                }

                // Выполняем заправку после завершения отсчета
                fuelManager.SubtractFuel(selectedFuelType, quantity, paymentType, selectedKolonka.Id);

                // Освобождаем колонку после завершения
                SetPumpBusy(pumpNumber, false);

            }
            catch (Exception ex)
            {
                // Восстанавливаем кнопку в случае ошибки
                RefuelButton.IsEnabled = true;
                RefuelButton.Content = "Заправить";

                // Освобождаем колонку в случае ошибки
                int pumpNumber = GetPumpNumberFromKolonka(selectedKolonka);
                SetPumpBusy(pumpNumber, false);

                MessageBox.Show($"Ошибка при заправке: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Методы для управления занятостью колонок
        private bool IsPumpBusy(int pumpNumber)
        {
            return busyPumps.ContainsKey(pumpNumber) && busyPumps[pumpNumber];
        }

        private void SetPumpBusy(int pumpNumber, bool isBusy)
        {
            if (isBusy)
                busyPumps[pumpNumber] = true;
            else
                busyPumps.Remove(pumpNumber);
        }

        private int GetPumpNumberFromKolonka(KolonkaItem kolonka)
        {
            if (kolonka.KolonkaNumber.StartsWith("Колонка "))
            {
                string numberStr = kolonka.KolonkaNumber.Substring(8);
                if (int.TryParse(numberStr, out int number))
                {
                    return number;
                }
            }
            return 1;
        }

        private void KolonkaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KolonkaComboBox.SelectedItem is KolonkaItem selectedKolonka)
            {
                foreach (ComboBoxItem item in FuelTypeComboBox.Items)
                {
                    if (item.Content.ToString() == selectedKolonka.FuelType)
                    {
                        FuelTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void FuelTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}