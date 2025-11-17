using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BuildHub.Services;
using System.Windows.Controls;
using System;
using System.Net.Http;

namespace BuildHub
{
    public partial class MainWindow : Window
    {
        private const string PlaceholderText = "Расскажите нам о своих возможностях";
        private AiServiceFactory _aiServiceFactory;
        private IAiService? _currentAiService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAiServices();
        }

        private void InitializeAiServices()
        {
            var configService = ConfigurationService.Instance;
            var config = configService.GetApiConfig();

            _aiServiceFactory = new AiServiceFactory(config);
            _currentAiService = _aiServiceFactory.CreateService(AiProvider.ChatGPT);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AgentBuilderButton_Click(object sender, RoutedEventArgs e)
        {
            var agentBuilderWindow = new AgentBuilderWindow();
            agentBuilderWindow.Show();
        }

        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (InputTextBox.Text == PlaceholderText)
            {
                InputTextBox.Text = string.Empty;
                InputTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                InputTextBox.Text = PlaceholderText;
                InputTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (InputTextBox.Text == PlaceholderText || string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите ваш вопрос", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userMessage = InputTextBox.Text;

            // Обновляем провайдера на основе выбора
            UpdateAiProvider();

            // Отключаем кнопку и поле ввода во время запроса
            SendButton.IsEnabled = false;
            InputTextBox.IsEnabled = false;
            InputTextBox.Text = "Отправка запроса...";
            InputTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));

            try
            {
                if (_currentAiService != null)
                {
                    var response = await _currentAiService.SendMessageAsync(userMessage);

                    // Показываем ответ
                    MessageBox.Show($"Ответ от {_currentAiService.GetServiceName()}:\n\n{response}",
                                  "Ответ AI", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("AI сервис не инициализирован", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Запрос был отменен", "Отменено",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Некорректные данные: {ex.Message}", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка сети при обращении к API:\n{ex.Message}\n\nПроверьте подключение к интернету и API ключи.",
                              "Сетевая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Ошибка обработки ответа API:\n{ex.Message}\n\nВозможно, формат ответа API изменился.",
                              "Ошибка обработки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка:\n{ex.Message}\n\nТип: {ex.GetType().Name}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем состояние
                SendButton.IsEnabled = true;
                InputTextBox.IsEnabled = true;
                InputTextBox.Text = PlaceholderText;
                InputTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            }
        }

        private void UpdateAiProvider()
        {
            var selectedIndex = AiProviderComboBox.SelectedIndex;
            var provider = selectedIndex switch
            {
                0 => AiProvider.ChatGPT,
                1 => AiProvider.DeepSeek,
                2 => AiProvider.YandexGPT,
                _ => AiProvider.ChatGPT
            };

            _currentAiService = _aiServiceFactory.CreateService(provider);
        }
    }
}
