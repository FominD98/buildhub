using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BuildHub.Services;
using System.Windows.Controls;
using System;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
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
        private readonly AgentManager _agentManager;
        private readonly AgentExecutor _agentExecutor;
        private string _currentSearchQuery = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            _projectManager = ProjectManager.Instance;
            _agentManager = AgentManager.Instance;
            _agentExecutor = new AgentExecutor();
            InitializeAiServices();
            LoadProjects();
            LoadAgents();
        }

        private void InitializeAiServices()
        {
            var configService = ConfigurationService.Instance;
            var config = configService.GetApiConfig();

            _aiServiceFactory = new AiServiceFactory(config);
            _currentAiService = _aiServiceFactory.CreateService(AiProvider.ChatGPT);
        }

        private void LoadAgents()
        {
            var agents = _agentManager.GetAllAgents();

            // Создаем специальный элемент "Без агента"
            var agentList = new List<object> { new { Id = Guid.Empty, Name = "Без агента (прямой запрос)" } };
            agentList.AddRange(agents);

            AgentComboBox.ItemsSource = agentList;
            AgentComboBox.SelectedIndex = 0; // По умолчанию "Без агента"
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

            // Отключаем кнопку и поле ввода во время запроса
            SendButton.IsEnabled = false;
            InputTextBox.IsEnabled = false;
            AgentComboBox.IsEnabled = false;
            AiProviderComboBox.IsEnabled = false;
            InputTextBox.Text = "Отправка запроса...";
            InputTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));

            try
            {
                string response;
                string sourceInfo;

                // Проверяем, выбран ли агент
                var selectedAgentValue = AgentComboBox.SelectedValue;
                if (selectedAgentValue is Guid agentId && agentId != Guid.Empty)
                {
                    // Используем агента
                    var agent = _agentManager.GetAgent(agentId);
                    if (agent != null)
                    {
                        var task = await _agentExecutor.ExecuteTaskAsync(agent, userMessage);

                        if (task.Status == TaskStatus.Completed)
                        {
                            response = task.Response ?? "Нет ответа";
                            sourceInfo = $"Агент: {agent.Name}";
                        }
                        else if (task.Status == TaskStatus.Failed)
                        {
                            MessageBox.Show($"Ошибка выполнения задачи агентом:\n{task.ErrorMessage}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            MessageBox.Show("Задача была отменена", "Отменено",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Выбранный агент не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // Прямой запрос без агента
                    UpdateAiProvider();

                    if (_currentAiService != null)
                    {
                        response = await _currentAiService.SendMessageAsync(userMessage);
                        sourceInfo = $"Провайдер: {_currentAiService.GetServiceName()}";
                    }
                    else
                    {
                        MessageBox.Show("AI сервис не инициализирован", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Показываем ответ
                MessageBox.Show($"{sourceInfo}\n\n{response}",
                    "Ответ AI", MessageBoxButton.OK, MessageBoxImage.Information);
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
                AgentComboBox.IsEnabled = true;
                AiProviderComboBox.IsEnabled = true;
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

        private void AiProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Когда меняется провайдер, сбрасываем выбор агента на "Без агента"
            if (AgentComboBox != null)
            {
                AgentComboBox.SelectedIndex = 0;
            }
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
                Foreground = Brushes.White,
                Padding = new Thickness(16, 10, 16, 10),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40FFFFFF"))
            };

            // Градиентный фон как в других кнопках
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFF5F0"), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFDAB9"), 0.5));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#EC4899"), 1));
            createButton.Background = gradientBrush;

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
            // Защита от вызова во время инициализации XAML
            if (_projectManager == null)
                return;

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
