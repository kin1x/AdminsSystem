using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace AdminSystem
{
    public partial class MainWindow : Window
    {
        private readonly string databasePath = "Data Source=Data/Admins.db";

        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик события нажатия кнопки "Войти"
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // Получаем введенный пользователем логин и пароль
            string username = LoginUsernameTextBox.Text;
            string password = LoginPasswordTextBox.Password;

            // Проверяем, что логин и пароль не пустые
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                // Выводим сообщение об ошибке, если логин или пароль пустые
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            // Проверяем аутентификацию пользователя с помощью функции AuthenticateUser,
            // передавая ей введенный логин и хэшированный пароль
            if (AuthenticateUser(username, HashPassword(password)))
            {
                // Если аутентификация успешна, регистрируем действие в журнале, выводим сообщение об успешном входе
                // и переключаем видимость панелей: панель администратора становится видимой, а панель входа скрывается
                LogAction(username, "Вход в систему");
                MessageBox.Show("Успешный вход");
                AdminPanel.Visibility = Visibility.Visible;
                LoginPanel.Visibility = Visibility.Collapsed;

                // Обновляем дату и время последнего входа пользователя
                UpdateLastLoginDateTime(username);
            }
            else
            {
                // Если аутентификация неуспешна, выводим сообщение об ошибке
                MessageBox.Show("Неверный логин или пароль");
            }
        }

        // Обработчик нажатия кнопки "Журнал"
        private void ViewActionLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем список строк для хранения журнала действий администраторов
                List<string> actionLogs = new List<string>();

                // Создаем новое подключение к базе данных SQLite
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение с базой данных
                    connection.Open();

                    // SQL-запрос для извлечения логина и журнала действий администраторов
                    string query = "SELECT Username, ActionLog FROM Administrators WHERE ActionLog IS NOT NULL";

                    // Создаем новую команду SQL с запросом и подключением
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        // Читаем результаты запроса построчно
                        while (reader.Read())
                        {
                            // Извлекаем логин и журнал действий администраторов из результатов чтения
                            string username = reader["Username"].ToString();
                            string actionLog = reader["ActionLog"].ToString();

                            // Формируем строку вида "логин: журнал действий" и добавляем её в список
                            actionLogs.Add($"{username}: {actionLog}");
                        }
                    }
                }

                // Объединяем все строки из списка в одну строку с разделителем перевода строки
                string logContent = string.Join("\n", actionLogs);

                // Отображаем содержимое журнала действий администраторов в сообщении
                MessageBox.Show(logContent, "Журнал действий администраторов", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение с информацией об ошибке
                MessageBox.Show($"Произошла ошибка при извлечении журнала действий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Функция аутентификации пользователя
        private bool AuthenticateUser(string username, string hashedPassword)
        {
            // Создаем новое подключение к базе данных SQLite
            using (SQLiteConnection connection = new SQLiteConnection(databasePath))
            {
                // Открываем соединение с базой данных
                connection.Open();

                // SQL-запрос для проверки существования пользователя с указанным логином и паролем
                string query = "SELECT COUNT(*) FROM Administrators WHERE Username=@Username AND Password=@Password";

                // Создаем новую команду SQL с запросом и подключением
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    // Добавляем параметры (логин и хэшированный пароль) к команде SQL
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    // Выполняем запрос и получаем количество найденных записей
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    // Возвращаем результат: true, если найдена хотя бы одна запись (аутентификация успешна),
                    // иначе false (аутентификация неуспешна)
                    return count > 0;
                }
            }
        }

        // Обработчик события нажатия кнопки "Зарегистрироваться"
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем логин и пароль из соответствующих текстовых полей
                string username = LoginUsernameTextBox.Text;
                string password = LoginPasswordTextBox.Password;

                // Проверяем, что логин и пароль не пустые или состоят только из пробелов
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    // Выводим сообщение об ошибке и выходим из метода
                    MessageBox.Show("Введите логин и пароль");
                    return;
                }

                // Проверяем, что логин не занят другим пользователем
                if (IsUsernameTaken(username))
                {
                    // Выводим сообщение об ошибке и выходим из метода
                    MessageBox.Show("Этот логин уже занят");
                    return;
                }

                // Хэшируем пароль
                string hashedPassword = HashPassword(password);

                // Регистрируем нового администратора в базе данных
                RegisterNewAdministrator(username, hashedPassword);

                // Выводим сообщение об успешной регистрации
                MessageBox.Show("Регистрация успешна");

                // Изменяем видимость панелей: панель входа скрываем, панель администратора отображаем
                LoginPanel.Visibility = Visibility.Collapsed;
                AdminPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // В случае возникновения ошибки выводим сообщение об ошибке
                MessageBox.Show($"Произошла ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для регистрации нового администратора
        private void RegisterNewAdministrator(string username, string hashedPassword)
        {
            try
            {
                // Создаем подключение к базе данных SQLite
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // SQL-запрос для добавления записи о новом администраторе в таблицу Administrators
                    string query = "INSERT INTO Administrators (Username, Password, RegistrationDateTime) VALUES (@Username, @Password, @RegistrationDateTime)";

                    // Создаем команду SQL с параметрами
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметры к команде
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", hashedPassword);
                        command.Parameters.AddWithValue("@RegistrationDateTime", DateTime.Now);

                        // Выполняем команду SQL
                        command.ExecuteNonQuery();
                    }
                }

                // Записываем действие в лог
                LogAction(username, "Регистрация нового администратора");
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            }
        }

        // Метод для проверки занятости логина
        private bool IsUsernameTaken(string username)
        {
            try
            {
                // Создаем подключение к базе данных SQLite
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // SQL-запрос для проверки количества записей с указанным логином в таблице Administrators
                    string query = "SELECT COUNT(*) FROM Administrators WHERE Username=@Username";

                    // Создаем команду SQL с параметрами
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметр логина к команде
                        command.Parameters.AddWithValue("@Username", username);

                        // Выполняем команду SQL и получаем количество записей
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Возвращаем результат проверки: true, если логин занят, и false, если свободен
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке и возвращаем false
                MessageBox.Show($"Ошибка при проверке занятости логина: {ex.Message}");
                return false;
            }
        }

        // Метод для хеширования пароля
        private string HashPassword(string password)
        {
            try
            {
                // Создаем объект для вычисления хеша SHA256
                using (SHA256 sha256 = SHA256.Create())
                {
                    // Преобразуем пароль в массив байтов и вычисляем его хеш
                    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                    // Создаем объект StringBuilder для построения строки хеша
                    StringBuilder builder = new StringBuilder();

                    // Преобразуем каждый байт хеша в шестнадцатеричную строку и добавляем к строке builder
                    for (int i = 0; i < hashedBytes.Length; i++)
                    {
                        builder.Append(hashedBytes[i].ToString("x2"));
                    }

                    // Возвращаем строковое представление хеша
                    return builder.ToString();
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке и возвращаем пустую строку
                MessageBox.Show($"Ошибка при хешировании пароля: {ex.Message}");
                return string.Empty;
            }
        }

        // Метод для удаления администратора из базы данных
        private void DeleteAdministrator(string username)
        {
            try
            {
                // Устанавливаем соединение с базой данных
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // Запрос на удаление администратора из таблицы по его имени пользователя
                    string query = "DELETE FROM Administrators WHERE Username=@Username";

                    // Создаем команду с запросом и подключением к базе данных
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметр с именем пользователя
                        command.Parameters.AddWithValue("@Username", username);

                        // Выполняем команду для удаления записи из таблицы
                        command.ExecuteNonQuery();
                    }
                }

                // Записываем действие удаления администратора в лог
                LogAction(username, "Удаление администратора");
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при удалении администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для получения списка администраторов из базы данных
        private List<Administrator> GetAdministrators()
        {
            // Создаем список для хранения информации об администраторах
            List<Administrator> administrators = new List<Administrator>();

            try
            {
                // Устанавливаем соединение с базой данных
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // Запрос на выборку данных об администраторах из таблицы
                    string query = "SELECT AdminID, Username, Password, RegistrationDateTime, LastLoginDateTime, ActionLog FROM Administrators";

                    // Создаем команду с запросом и подключением к базе данных
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Используем объект reader для выполнения запроса и чтения результатов
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            // Читаем результаты запроса
                            while (reader.Read())
                            {
                                // Создаем объект администратора и заполняем его данными из результата запроса
                                Administrator admin = new Administrator
                                {
                                    AdminID = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    Password = reader.GetString(2),
                                    RegistrationDateTime = reader.GetDateTime(3),
                                    LastLoginDateTime = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4),
                                    ActionLog = reader.IsDBNull(5) ? null : reader.GetString(5)
                                };
                                // Добавляем администратора в список
                                administrators.Add(admin);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при получении списка администраторов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Возвращаем список администраторов
            return administrators;
        }

        // Метод для обновления информации об администраторе в базе данных
        private void UpdateAdministrator(string oldUsername, string newUsername, string newPassword)
        {
            try
            {
                // Удаляем администратора с текущим логином
                string username = LoginUsernameTextBox.Text;
                DeleteAdministrator(username);

                // Устанавливаем соединение с базой данных
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // Запрос на обновление данных администратора
                    string query = "UPDATE Administrators SET Username=@NewUsername, Password=@NewPassword WHERE Username=@OldUsername";

                    // Создаем команду с запросом и подключением к базе данных
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметры для запроса
                        command.Parameters.AddWithValue("@NewUsername", newUsername);
                        command.Parameters.AddWithValue("@NewPassword", HashPassword(newPassword));
                        command.Parameters.AddWithValue("@OldUsername", oldUsername);

                        // Выполняем запрос на обновление данных
                        command.ExecuteNonQuery();
                    }

                    // Записываем действие об удалении администратора в лог
                    LogAction(username, "Удаление администратора");
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при обновлении данных администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для обновления времени последнего входа администратора
        private void UpdateLastLoginDateTime(string username)
        {
            try
            {
                // Устанавливаем соединение с базой данных
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // Запрос на обновление времени последнего входа администратора
                    string query = "UPDATE Administrators SET LastLoginDateTime=@LastLoginDateTime WHERE Username=@Username";

                    // Создаем команду с запросом и подключением к базе данных
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметры для запроса
                        command.Parameters.AddWithValue("@LastLoginDateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@Username", username);

                        // Выполняем запрос на обновление времени последнего входа
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при обновлении времени последнего входа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для записи действия администратора в журнал действий
        private void LogAction(string username, string action)
        {
            try
            {
                // Устанавливаем соединение с базой данных
                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    // Открываем соединение
                    connection.Open();

                    // Запрос на добавление действия в журнал действий администратора
                    string query = "UPDATE Administrators SET ActionLog=COALESCE(ActionLog || '; ', '') || @Action WHERE Username=@Username";

                    // Создаем команду с запросом и подключением к базе данных
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        // Добавляем параметры для запроса
                        command.Parameters.AddWithValue("@Action", action);
                        command.Parameters.AddWithValue("@Username", username);

                        // Выполняем запрос на добавление действия в журнал действий
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Ошибка при записи действия в журнал: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для изменения логина и пароля администратора
        private void ChangeAdminUsernameAndPassword(string oldUsername, string newUsername, string newPassword)
        {
            try
            {
                // Обновляем информацию об администраторе
                UpdateAdministrator(oldUsername, newUsername, newPassword);

                // Записываем действие об изменении логина и пароля администратора в журнал действий
                LogAction(newUsername, "Изменение логина и пароля");

                // Выводим сообщение об успешном изменении данных администратора
                MessageBox.Show("Логин и пароль администратора успешно изменены.", "Изменение данных администратора", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                // В случае ошибки выводим сообщение об ошибке
                MessageBox.Show($"Произошла ошибка при изменении данных администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для отображения экрана аутентификации
        private void ShowAuthenticationScreen()
        {
            // Очистка полей логина и пароля
            LoginUsernameTextBox.Clear();
            LoginPasswordTextBox.Clear();

            // Скрытие панели администратора и отображение панели аутентификации
            AdminPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
        }

        // Обработчик нажатия кнопки удаления администратора
        private void DeleteAdministratorButton_Click(object sender, RoutedEventArgs e)
        {
            // Получение имени пользователя из поля ввода логина
            string username = LoginUsernameTextBox.Text;

            // Удаление администратора из базы данных
            DeleteAdministrator(username);

            // Очистка полей логина и пароля
            LoginUsernameTextBox.Clear();
            LoginPasswordTextBox.Clear();

            // Отображение экрана аутентификации
            ShowAuthenticationScreen();

            // Скрытие панели администратора и отображение панели аутентификации
            AdminPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;

            // Вывод сообщения об успешном удалении администратора
            MessageBox.Show($"Администратор {username} успешно удален.", "Удаление администратора", MessageBoxButton.OK);
        }

        // Обработчик нажатия кнопки просмотра логинов и паролей администраторов
        private void ViewLoginAndPasswordsButton_Click(object sender, RoutedEventArgs e)
        {
            // Получение списка администраторов из базы данных
            List<Administrator> administrators = GetAdministrators();

            // Проверка наличия зарегистрированных администраторов
            if (administrators.Count == 0)
            {
                // Вывод сообщения о пустом списке администраторов
                MessageBox.Show("Нет зарегистрированных администраторов.", "Пусто", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Создание строки для отображения информации о логинах и паролях администраторов
            StringBuilder adminInfo = new StringBuilder();
            foreach (Administrator admin in administrators)
            {
                adminInfo.AppendLine($"Username: {admin.Username}, Password: {admin.Password}");
            }

            // Вывод информации о логинах и паролях администраторов
            MessageBox.Show(adminInfo.ToString(), "Администраторы и их пароли", MessageBoxButton.OK);
        }

        // Обработчик нажатия кнопки изменения информации об администраторе
        private void ChangeAdminInfoButton_Click(object sender, RoutedEventArgs e)
        {
            // Получение старого логина, нового логина и нового пароля администратора
            string oldUsername = LoginUsernameTextBox.Text;
            string newUsername = NewUsernameTextBox.Text;
            string newPassword = NewPasswordTextBox.Password;

            // Вызов метода для изменения логина и пароля администратора
            ChangeAdminUsernameAndPassword(oldUsername, newUsername, newPassword);
        }

        // Обработчик нажатия кнопки выхода из системы
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Получение имени пользователя
            string username = LoginUsernameTextBox.Text;

            // Логирование действия выхода из системы
            LogAction(username, "Выход из системы");

            // Очистка полей ввода логина и пароля
            LoginUsernameTextBox.Clear();
            LoginPasswordTextBox.Clear();

            // Отображение экрана аутентификации
            ShowAuthenticationScreen();
        }
    }

    // Класс, представляющий администратора
    public class Administrator
    {
        // Идентификатор администратора
        public int AdminID { get; set; }

        // Логин администратора
        public string Username { get; set; }

        // Пароль администратора
        public string Password { get; set; }

        // Дата и время регистрации администратора
        public DateTime RegistrationDateTime { get; set; }

        // Дата и время последнего входа администратора (может быть null, если вход не был выполнен)
        public DateTime? LastLoginDateTime { get; set; }

        // Журнал действий администратора
        public string ActionLog { get; set; }
    }
}