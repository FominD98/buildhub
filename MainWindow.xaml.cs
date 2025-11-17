using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BuildHub.Services;
using System.Windows.Controls;
using System;
using System.Net.Http;
using System.Linq;
using BuildHub.Models;

namespace BuildHub
{
    public partial class MainWindow : Window
    {
        private const string PlaceholderText = "Расскажите нам о своих возможностях";
        private const string SearchPlaceholder = "Search";
        private AiServiceFactory _aiServiceFactory;
        private IAiService? _currentAiService;
        private readonly ProjectManager _projectManager;
        private string _currentSearchQuery = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            _projectManager = ProjectManager.Instance;
            InitializeAiServices();
            LoadProjects();
        }

        private void InitializeAiServices()
        {
            var configService = ConfigurationService.Instance;
            var config = configService.GetApiConfig();

            _aiServiceFactory = new AiServiceFactory(config);
            _currentAiService = _aiServiceFactory.CreateService(AiProvider.ChatGPT);
        }

        private void LoadProjects(string searchQuery = "")
        {
            var projects = _projectManager.GetAllProjects();

            // Фильтрация по поиску
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                projects = projects.Where(p =>
                    p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Группировка по датам
            var groupedProjects = projects
                .GroupBy(p => p.GetDateGroup())
                .Select(g => new { Key = g.Key, Value = g.ToList() })
                .ToList();

            ProjectsListControl.ItemsSource = groupedProjects;
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

        private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // Простой диалог для ввода имени проекта
            var dialog = new Window
            {
                Title = "Новый проект",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E2E")),
                WindowStyle = WindowStyle.ToolWindow
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var nameLabel = new TextBlock
            {
                Text = "Название проекта:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var nameTextBox = new TextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF")),
                Foreground = Brushes.White,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40FFFFFF")),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var descLabel = new TextBlock
            {
                Text = "Описание (необязательно):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descTextBox = new TextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF")),
                Foreground = Brushes.White,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40FFFFFF")),
                Margin = new Thickness(0, 0, 0, 20),
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };

            var createButton = new Button
            {
                Content = "Создать",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF")),
                Foreground = Brushes.White,
                Padding = new Thickness(12, 8, 12, 8),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            createButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show("Введите название проекта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _projectManager.CreateProject(nameTextBox.Text.Trim(), descTextBox.Text.Trim());
                LoadProjects(_currentSearchQuery);
                dialog.Close();
            };

            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(descLabel);
            stackPanel.Children.Add(descTextBox);
            stackPanel.Children.Add(createButton);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void DeleteProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is Guid projectId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот проект?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _projectManager.DeleteProject(projectId);
                    LoadProjects(_currentSearchQuery);
                }
            }
        }

        private void ProjectItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            // Обработчик правого клика для контекстного меню
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text;
            if (searchText == SearchPlaceholder)
            {
                _currentSearchQuery = string.Empty;
            }
            else
            {
                _currentSearchQuery = searchText;
            }

            LoadProjects(_currentSearchQuery);
        }
    }
}
