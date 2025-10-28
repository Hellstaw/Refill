using System;
using System.Collections.Generic;
using Npgsql;

namespace WpfApp2
{
    public class FuelManager
    {
        private string connectionString = "Host=localhost;Port=5432;Database=Kyrsovay;Username=postgres;Password=123";

        // Метод для получения доступных колонок
        public List<deletefuell.KolonkaItem> GetAvailableKolonki()
        {
            var kolonki = new List<deletefuell.KolonkaItem>();
            
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id_kolonki, kolonka_number, fuel_type FROM kolonki WHERE status_id = 1"; // статус 1 = Активна
                
                using (var command = new NpgsqlCommand(query, conn))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        kolonki.Add(new deletefuell.KolonkaItem
                        {
                            Id = reader.GetInt32(0),
                            KolonkaNumber = $"Колонка {reader.GetInt32(1)}",
                            FuelType = reader.GetString(2)
                        });
                    }
                }
            }
            return kolonki;
        }

        // Метод для списания топлива с записью в историю
        public void SubtractFuel(string fuelType, double quantity, string paymentType, int kolonkaId)
        {
            UpdateFuelQuantity(fuelType, quantity, false, paymentType, kolonkaId);
        }

        // Метод для добавления топлива
        public void AddFuel(string fuelType, double quantity)
        {
            UpdateFuelQuantity(fuelType, quantity, true);
        }

        // Универсальный метод для обновления количества топлива
        public void UpdateFuelQuantity(string fuelType, double quantity, bool isAddition = true, 
                                     string paymentType = null, int kolonkaId = 0)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Получаем информацию о текущем пользователе
                var app = (App)System.Windows.Application.Current;
                int workerId = app.CurrentWorkerId;
                string workerName = app.CurrentUsername;

                // Проверяем существование записи с таким типом топлива
                string checkQuery = "SELECT id_type, countfuel FROM typeoffuel WHERE typees = @typees";
                int? existingId = null;
                double existingQuantity = 0;

                using (var checkCommand = new NpgsqlCommand(checkQuery, conn))
                {
                    checkCommand.Parameters.AddWithValue("@typees", fuelType);

                    using (var reader = checkCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingId = reader.GetInt32(0);
                            existingQuantity = reader.GetDouble(1);
                        }
                    }
                }

                double newQuantity;
                if (isAddition)
                {
                    newQuantity = existingQuantity + quantity;
                }
                else
                {
                    // Проверяем, достаточно ли топлива для списания
                    if (existingQuantity < quantity)
                    {
                        throw new InvalidOperationException($"Недостаточно топлива! Доступно: {existingQuantity} л., требуется: {quantity} л.");
                    }
                    newQuantity = existingQuantity - quantity;
                }

                // Обновляем количество топлива
                if (existingId.HasValue)
                {
                    string updateQuery = @"
                        UPDATE typeoffuel 
                        SET countfuel = @newCount 
                        WHERE id_type = @id_type";

                    using (var updateCommand = new NpgsqlCommand(updateQuery, conn))
                    {
                        updateCommand.Parameters.AddWithValue("@newCount", newQuantity);
                        updateCommand.Parameters.AddWithValue("@id_type", existingId.Value);
                        updateCommand.ExecuteNonQuery();
                    }

                    // Если это списание топлива (заправка), записываем в историю
                    if (!isAddition)
                    {
                        // Получаем цену за литр
                        double pricePerLiter = GetFuelPrice(fuelType);
                        double totalPrice = pricePerLiter * quantity;

                        // Получаем номер колонки
                        string kolonkaNumber = GetKolonkaNumber(kolonkaId);

                        string insertHistoryQuery = @"
                            INSERT INTO history 
                            (worker_id, worker_name, kolonka_id, operation_date, payment_type, total_amount, fuel_type, quantity) 
                            VALUES (@workerId, @workerName, @kolonkaId, @operationDate, @paymentType, @totalAmount, @fuelType, @quantity)";

                        using (var historyCommand = new NpgsqlCommand(insertHistoryQuery, conn))
                        {
                            historyCommand.Parameters.AddWithValue("@workerId", workerId);
                            historyCommand.Parameters.AddWithValue("@workerName", workerName);
                            historyCommand.Parameters.AddWithValue("@kolonkaId", kolonkaId);
                            historyCommand.Parameters.AddWithValue("@operationDate", DateTime.Now);
                            historyCommand.Parameters.AddWithValue("@paymentType", paymentType);
                            historyCommand.Parameters.AddWithValue("@totalAmount", totalPrice);
                            historyCommand.Parameters.AddWithValue("@fuelType", fuelType);
                            historyCommand.Parameters.AddWithValue("@quantity", quantity);
                            historyCommand.ExecuteNonQuery();
                        }

                        // Показываем сообщение об успехе
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show($"Заправка успешно выполнена!\n" +
                                                          $"Колонка: {kolonkaNumber}\n" +
                                                          $"Тип топлива: {fuelType}\n" +
                                                          $"Литров: {quantity}\n" +
                                                          $"Тип оплаты: {paymentType}\n" +
                                                          $"Сумма: {totalPrice} руб.\n" +
                                                          $"Было: {existingQuantity} л.\n" +
                                                          $"Осталось: {newQuantity} л.",
                                                          "Успех",
                                                          System.Windows.MessageBoxButton.OK,
                                                          System.Windows.MessageBoxImage.Information);
                        });
                    }
                    else
                    {
                        // Сообщение для пополнения
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show($"Топливо успешно добавлено!\n" +
                                                          $"Тип: {fuelType}\n" +
                                                          $"Добавлено: {quantity} л.\n" +
                                                          $"Было: {existingQuantity} л.\n" +
                                                          $"Стало: {newQuantity} л.",
                                                          "Успех",
                                                          System.Windows.MessageBoxButton.OK,
                                                          System.Windows.MessageBoxImage.Information);
                        });
                    }
                }
                else
                {
                    if (!isAddition)
                    {
                        throw new InvalidOperationException("Нельзя списать топливо: запись не найдена в базе данных");
                    }
                    else
                    {
                        // Создаем новую запись для пополнения
                        string insertQuery = @"
                            INSERT INTO typeoffuel (countfuel, typees) 
                            VALUES (@countfuel, @typees)";

                        using (var insertCommand = new NpgsqlCommand(insertQuery, conn))
                        {
                            insertCommand.Parameters.AddWithValue("@countfuel", newQuantity);
                            insertCommand.Parameters.AddWithValue("@typees", fuelType);
                            insertCommand.ExecuteNonQuery();
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show($"Топливо успешно добавлено!\n" +
                                                          $"Тип: {fuelType}\n" +
                                                          $"Добавлено: {quantity} л.\n" +
                                                          $"Стало: {newQuantity} л.",
                                                          "Успех",
                                                          System.Windows.MessageBoxButton.OK,
                                                          System.Windows.MessageBoxImage.Information);
                        });
                    }
                }
            }
        }

        // Метод для получения номера колонки по ID
        private string GetKolonkaNumber(int kolonkaId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT kolonka_number FROM kolonki WHERE id_kolonki = @id";

                using (var command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@id", kolonkaId);
                    var result = command.ExecuteScalar();
                    return result != null ? $"Колонка {result}" : "Неизвестная колонка";
                }
            }
        }

        // Метод для получения цены топлива
        public double GetFuelPrice(string fuelType)
        {
            // Временные цены - можно вынести в таблицу БД
            switch (fuelType)
            {
                case "92": return 45.50;
                case "95": return 48.90;
                case "ДТ": return 52.30;
                default: return 50.00;
            }
        }

        // Метод для получения текущего количества топлива по типу
        public double GetFuelQuantity(string fuelType)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT countfuel FROM typeoffuel WHERE typees = @typees";

                using (var command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@typees", fuelType);

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToDouble(result);
                    }
                }
            }
            return 0;
        }

        // Метод для получения истории операций
        // Метод для получения истории операций
        public System.Data.DataTable GetOperationHistory()
        {
            var dataTable = new System.Data.DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
            SELECT 
                TO_CHAR(h.operation_date, 'DD.MM.YYYY HH24:MI') as ""Дата и время"",
                h.worker_name as ""Работник"",
                k.kolonka_number as ""Колонка"",
                h.fuel_type as ""Тип топлива"",
                h.quantity as ""Количество"",
                h.total_amount as ""Сумма"",
                h.payment_type as ""Тип оплаты""
            FROM history h
            LEFT JOIN kolonki k ON h.kolonka_id = k.id_kolonki
            ORDER BY h.operation_date DESC";

                using (var command = new NpgsqlCommand(sql, conn))
                using (var adapter = new NpgsqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }
    }
}