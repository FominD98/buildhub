using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BuildHub.Services;
using System.Windows.Controls;
using System;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildHub.Models;
using System.Collections.ObjectModel;

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
        private List<Guid> _selectedAgentIds = new List<Guid>();
        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();

        public MainWindow()
        {
            InitializeComponent();
            _projectManager = ProjectManager.Instance;
            _agentManager = AgentManager.Instance;
            _agentExecutor = new AgentExecutor();
            InitializeAiServices();
            LoadProjects();
            LoadAgents();
            InitializeChat();
        }

        private void InitializeChat()
        {
            ChatMessagesControl.ItemsSource = _chatMessages;
            // Не добавляем приветственное сообщение, чтобы показывать стартовую страницу
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

            // Очищаем панель чекбоксов
            AgentCheckboxesPanel.Children.Clear();

            // Создаем чекбоксы для каждого агента
            foreach (var agent in agents)
            {
                var checkbox = new CheckBox
                {
                    Content = agent.Name,
                    Tag = agent.Id,
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 15, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };

                checkbox.Checked += AgentCheckbox_Changed;
                checkbox.Unchecked += AgentCheckbox_Changed;

                AgentCheckboxesPanel.Children.Add(checkbox);
            }
        }

        private void AgentCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            // Обновляем список выбранных агентов
            _selectedAgentIds.Clear();

            foreach (CheckBox checkbox in AgentCheckboxesPanel.Children)
            {
                if (checkbox.IsChecked == true && checkbox.Tag is Guid agentId)
                {
                    _selectedAgentIds.Add(agentId);
                }
            }
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
            if (sender is TextBox textBox)
            {
                var placeholderToCheck = textBox.Name == "ChatInputTextBox" ? "Введите ваше сообщение..." : PlaceholderText;

                if (textBox.Text == placeholderToCheck)
                {
                    textBox.Text = string.Empty;
                    textBox.Foreground = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var placeholderToUse = textBox.Name == "ChatInputTextBox" ? "Введите ваше сообщение..." : PlaceholderText;

                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholderToUse;
                    textBox.Foreground = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Определяем, какой TextBox используется (стартовая страница или чат)
            TextBox activeTextBox = null;
            Button activeButton = null;

            if (InputTextBox != null && InputTextBox.IsVisible)
            {
                activeTextBox = InputTextBox;
                activeButton = SendButton;
            }
            else if (ChatInputTextBox != null && ChatInputTextBox.IsVisible)
            {
                activeTextBox = ChatInputTextBox;
                activeButton = ChatSendButton;
            }
            else
            {
                return;
            }

            if (activeTextBox.Text == PlaceholderText || string.IsNullOrWhiteSpace(activeTextBox.Text))
            {
                return;
            }

            var userMessage = activeTextBox.Text;

            // Добавляем сообщение пользователя в чат
            _chatMessages.Add(new ChatMessage(MessageRole.User, userMessage));

            // Очищаем поле ввода
            activeTextBox.Text = string.Empty;

            // Отключаем кнопку и поле ввода во время запроса
            if (activeButton != null) activeButton.IsEnabled = false;
            if (activeTextBox != null) activeTextBox.IsEnabled = false;
            if (AiProviderComboBox != null) AiProviderComboBox.IsEnabled = false;
            if (AgentCheckboxesPanel != null) AgentCheckboxesPanel.IsEnabled = false;

            // Добавляем индикатор "думает"
            var thinkingMessage = new ChatMessage(MessageRole.Assistant, "Обрабатываю запрос...")
            {
                IsThinking = true
            };
            _chatMessages.Add(thinkingMessage);

            // Прокручиваем вниз
            ScrollToBottom();

            try
            {

                // Удаляем индикатор "думает"
                _chatMessages.Remove(thinkingMessage);

                // Проверяем, выбрано ли несколько агентов
                if (_selectedAgentIds.Count > 1)
                {
                    // Множественное выполнение агентов параллельно
                    var tasks = new List<Task<(string AgentName, AgentTask Task)>>();

                    foreach (var agentId in _selectedAgentIds)
                    {
                        var agent = _agentManager.GetAgent(agentId);
                        if (agent != null)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                var task = await _agentExecutor.ExecuteTaskAsync(agent, userMessage);
                                return (agent.Name, task);
                            }));
                        }
                    }

                    var results = await Task.WhenAll(tasks);

                    // Добавляем ответы от каждого агента в чат
                    foreach (var (agentName, task) in results)
                    {
                        string content;
                        if (task.Status == Models.TaskStatus.Completed)
                        {
                            content = task.Response ?? "Нет ответа";
                        }
                        else if (task.Status == Models.TaskStatus.Failed)
                        {
                            content = $"❌ Ошибка: {task.ErrorMessage}";
                        }
                        else
                        {
                            content = "⚠️ Задача была отменена";
                        }

                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, content, agentName));
                        ScrollToBottom();
                    }

                    return; // Выходим из метода, сообщения уже добавлены
                }
                // Проверяем, выбран ли один агент
                else if (_selectedAgentIds.Count == 1)
                {
                    // Используем выбранного агента из списка
                    var agent = _agentManager.GetAgent(_selectedAgentIds[0]);
                    if (agent != null)
                    {
                        var task = await _agentExecutor.ExecuteTaskAsync(agent, userMessage);

                        string content;
                        if (task.Status == Models.TaskStatus.Completed)
                        {
                            content = task.Response ?? "Нет ответа";
                        }
                        else if (task.Status == Models.TaskStatus.Failed)
                        {
                            content = $"❌ Ошибка: {task.ErrorMessage}";
                        }
                        else
                        {
                            content = "⚠️ Задача была отменена";
                        }

                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, content, agent.Name));
                        ScrollToBottom();
                        return;
                    }
                    else
                    {
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "❌ Выбранный агент не найден"));
                        ScrollToBottom();
                        return;
                    }
                }
                // Нет выбранных агентов - прямой запрос к AI провайдеру
                else
                {
                    UpdateAiProvider();

                    if (_currentAiService != null)
                    {
                        var response = await _currentAiService.SendMessageAsync(userMessage);
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, response));
                    }
                    else
                    {
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "❌ AI сервис не инициализирован"));
                    }

                    ScrollToBottom();
                }
            }
            catch (OperationCanceledException)
            {
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "⚠️ Запрос был отменен"));
                ScrollToBottom();
            }
            catch (ArgumentException ex)
            {
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant, $"❌ Некорректные данные: {ex.Message}"));
                ScrollToBottom();
            }
            catch (HttpRequestException ex)
            {
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Ошибка сети: {ex.Message}\n\nПроверьте подключение к интернету и API ключи."));
                ScrollToBottom();
            }
            catch (InvalidOperationException ex)
            {
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Ошибка обработки: {ex.Message}"));
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Непредвиденная ошибка: {ex.Message}"));
                ScrollToBottom();
            }
            finally
            {
                // Восстанавливаем состояние всех контролов
                if (SendButton != null) SendButton.IsEnabled = true;
                if (ChatSendButton != null) ChatSendButton.IsEnabled = true;
                if (InputTextBox != null) InputTextBox.IsEnabled = true;
                if (ChatInputTextBox != null) ChatInputTextBox.IsEnabled = true;
                if (AiProviderComboBox != null) AiProviderComboBox.IsEnabled = true;
                if (AgentCheckboxesPanel != null) AgentCheckboxesPanel.IsEnabled = true;
            }
        }

        private void ScrollToBottom()
        {
            if (ChatScrollViewer != null)
            {
                ChatScrollViewer.ScrollToEnd();
            }
        }

        private void UpdateAiProvider()
        {
            // Защита от вызова во время инициализации XAML
            if (AiProviderComboBox == null || _aiServiceFactory == null)
                return;

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
            // Обновляем текущий AI провайдер
            UpdateAiProvider();
        }

        private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateProjectWindow();
            if (createWindow.ShowDialog() == true)
            {
                LoadProjects(_currentSearchQuery);
            }
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
