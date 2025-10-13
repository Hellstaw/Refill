using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp2
{
    public partial class LoginReg : Window
    {
        public LoginReg()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;
            connectionBD connection = new connectionBD();

            // Сбрасываем цвет границ
            txtUsername.BorderBrush = Brushes.Gray;
            txtPassword.BorderBrush = Brushes.Gray;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.BorderBrush = Brushes.Red;
                txtPassword.BorderBrush = Brushes.Red;
                return;
            }

            // Получаем результат валидации
            var validationResult = connection.ValidatedUser(username, password);

            if (validationResult.isValid)
            {
                // Записываем время входа
                connection.LogLogin(
                    validationResult.workerId,
                    validationResult.loginId,
                    validationResult.firstName,
                    validationResult.secondName
                );

                // Сохраняем информацию о пользователе в App
                var app = (App)Application.Current;
                app.CurrentWorkerId = validationResult.workerId;
                app.CurrentLoginId = validationResult.loginId;
                app.CurrentUsername = $"{validationResult.firstName} {validationResult.secondName}";

                // Открываем главное окно
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                txtUsername.BorderBrush = Brushes.Red;
                txtPassword.BorderBrush = Brushes.Red;
            }
        }
    }
}