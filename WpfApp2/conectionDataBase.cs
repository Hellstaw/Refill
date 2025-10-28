using Npgsql;
using System;
using System.Data;
using System.Windows;

namespace WpfApp2
{
    public class connectionBD
    {
        private string connectionString = "Host=localhost;Port=5432;Database=Kyrsovay;Username=postgres;Password=123";

        public DataTable GetFuel()
        {
            DataTable dataTable = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM history";
                using (var command = new NpgsqlCommand(sql, conn))
                using (var adapter = new NpgsqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        // Добавляем метод для получения истории операций
        public DataTable GetOperationHistory()
        {
            DataTable dataTable = new DataTable();
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

        // Остальные методы остаются без изменений
        public (bool isValid, int workerId, int loginId, string firstName, string secondName) ValidatedUser(string login, string password)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        w.id_worker,
                        a.id_login,
                        w.first_name,
                        w.second_name
                    FROM autorisation a
                    JOIN workers w ON a.id_login = w.login_id
                    WHERE a.login = @login AND a.passwords = @password";

                using (var command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int workerId = reader.GetInt32(0);
                            int loginId = reader.GetInt32(1);
                            string firstName = reader.GetString(2);
                            string secondName = reader.GetString(3);
                            return (true, workerId, loginId, firstName, secondName);
                        }
                    }
                }
            }
            return (false, 0, 0, string.Empty, string.Empty);
        }

        public void LogLogin(int workerId, int loginId, string firstName, string secondName)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO user_sessions (worker_id, login_id, username, login_time) 
                        VALUES (@workerId, @loginId, @username, @loginTime)";

                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@workerId", workerId);
                        command.Parameters.AddWithValue("@loginId", loginId);
                        command.Parameters.AddWithValue("@username", $"{firstName} {secondName}");
                        command.Parameters.AddWithValue("@loginTime", DateTime.Now);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи времени входа: {ex.Message}");
            }
        }

        public void LogLogout(int workerId, int loginId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string findSessionQuery = @"
                SELECT id 
                FROM user_sessions 
                WHERE worker_id = @workerId 
                AND login_id = @loginId
                AND logout_time IS NULL 
                ORDER BY login_time DESC 
                LIMIT 1";

                    int? sessionId = null;

                    using (var findCommand = new NpgsqlCommand(findSessionQuery, conn))
                    {
                        findCommand.Parameters.AddWithValue("@workerId", workerId);
                        findCommand.Parameters.AddWithValue("@loginId", loginId);

                        var result = findCommand.ExecuteScalar();
                        if (result != null)
                        {
                            sessionId = Convert.ToInt32(result);
                        }
                    }

                    if (sessionId.HasValue)
                    {
                        string updateQuery = @"
                    UPDATE user_sessions 
                    SET logout_time = @logoutTime 
                    WHERE id = @sessionId";

                        using (var updateCommand = new NpgsqlCommand(updateQuery, conn))
                        {
                            updateCommand.Parameters.AddWithValue("@sessionId", sessionId.Value);
                            updateCommand.Parameters.AddWithValue("@logoutTime", DateTime.Now);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertQuery = @"
                    INSERT INTO user_sessions (worker_id, login_id, username, login_time, logout_time) 
                    VALUES (@workerId, @loginId, 'Auto-generated', @logoutTime, @logoutTime)";

                        using (var insertCommand = new NpgsqlCommand(insertQuery, conn))
                        {
                            insertCommand.Parameters.AddWithValue("@workerId", workerId);
                            insertCommand.Parameters.AddWithValue("@loginId", loginId);
                            insertCommand.Parameters.AddWithValue("@logoutTime", DateTime.Now);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи времени выхода: {ex.Message}");
            }
        }

        public DataTable GetSessionHistory()
        {
            DataTable dataTable = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        us.id as session_id,
                        us.worker_id,
                        us.login_id,
                        us.username,
                        us.login_time,
                        us.logout_time,
                        CASE 
                            WHEN us.logout_time IS NULL THEN 
                                EXTRACT(EPOCH FROM (NOW() - us.login_time)) / 60
                            ELSE 
                                EXTRACT(EPOCH FROM (us.logout_time - us.login_time)) / 60
                        END as duration_minutes
                    FROM user_sessions us 
                    ORDER BY us.login_time DESC";

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