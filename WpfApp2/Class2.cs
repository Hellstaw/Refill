using System;
using Npgsql;

namespace WpfApp2
{
    public class FuelManager
    {
        private string connectionString = "Host=localhost;Port=5432;Database=Kyrsovay;Username=postgres;Password=123";

        // Универсальный метод для добавления или вычитания топлива
        public void UpdateFuelQuantity(string fuelType, double quantity, bool isAddition = true)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Проверяем, существует ли уже запись с таким типом топлива
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
                        throw new InvalidOperationException($"Недостаточно топлива! Доступно: {existingQuantity}, требуется: {quantity}");
                    }
                    newQuantity = existingQuantity - quantity;
                }

                // Если запись существует - обновляем, иначе создаем новую
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
                }
                else
                {
                    // Если пытаемся вычесть из несуществующей записи
                    if (!isAddition)
                    {
                        throw new InvalidOperationException("Нельзя списать топливо: запись не найдена в базе данных");
                    }

                    string insertQuery = @"
                        INSERT INTO typeoffuel (countfuel, typees) 
                        VALUES (@countfuel, @typees)";

                    using (var insertCommand = new NpgsqlCommand(insertQuery, conn))
                    {
                        insertCommand.Parameters.AddWithValue("@countfuel", newQuantity);
                        insertCommand.Parameters.AddWithValue("@typees", fuelType);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        // Удобные методы-обертки для конкретных операций
        public void AddFuel(string fuelType, double quantity)
        {
            UpdateFuelQuantity(fuelType, quantity, true);
        }

        public void SubtractFuel(string fuelType, double quantity)
        {
            UpdateFuelQuantity(fuelType, quantity, false);
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
    }
}