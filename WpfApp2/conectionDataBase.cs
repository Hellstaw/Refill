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
                string sql = "SELECT * FROM workers";
                using (var command = new NpgsqlCommand(sql, conn))
                using (var adapter = new NpgsqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        // Обновленный метод валидации - возвращает ID работника и ID логина
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

        // Метод для записи времени входа
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

        // Метод для записи времени выхода (надежная версия)
        public void LogLogout(int workerId, int loginId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Сначала находим ID последней активной сессии
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

                    // Если нашли сессию, обновляем ее
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
                        // Если не нашли активной сессии, можно записать новую с обоими временами
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

        // Метод для получения истории сессий
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
        //public DataTable GetAvailableFuel()
        //{
        //    DataTable dataTable = new DataTable();
        //    using (var conn = new NpgsqlConnection(connectionString))
        //    {
        //        conn.Open();

        //        // Предполагаемая структура - адаптируйте под вашу БД
        //        string sql = @"SELECT 
        //              idfuel, 
        //              typefuel, 
        //              quantity,
        //              price  -- если есть цена
        //              FROM fuel 
        //              WHERE quantity > 0 
        //              ORDER BY typefuel";

        //        using (var command = new NpgsqlCommand(sql, conn))
        //        using (var adapter = new NpgsqlDataAdapter(command))
        //        {
        //            adapter.Fill(dataTable);
        //        }
        //    }
        //    return dataTable;
        //}
    }
}