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

        // Система хранения истории чатов для каждого проекта
        private Dictionary<Guid, ObservableCollection<ChatMessage>> _projectChatHistories = new Dictionary<Guid, ObservableCollection<ChatMessage>>();
        private Guid? _currentProjectId = null;

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

        private void UpdateChatVisibility()
        {
            if (WelcomeGrid != null && ChatGrid != null)
            {
                if (_chatMessages.Count > 0)
                {
                    WelcomeGrid.Visibility = Visibility.Collapsed;
                    ChatGrid.Visibility = Visibility.Visible;
                    Console.WriteLine($"✓ Switched to Chat view. Messages: {_chatMessages.Count}");
                }
                else
                {
                    WelcomeGrid.Visibility = Visibility.Visible;
                    ChatGrid.Visibility = Visibility.Collapsed;
                    Console.WriteLine($"✓ Switched to Welcome view. Messages: {_chatMessages.Count}");
                }
            }
        }

        private void SwitchToProject(Guid projectId)
        {
            Console.WriteLine($"=== SwitchToProject called: {projectId} ===");

            // Сохраняем текущую историю чата (если есть активный проект)
            if (_currentProjectId.HasValue)
            {
                Console.WriteLine($"  Saving chat history for project: {_currentProjectId.Value}");
                _projectChatHistories[_currentProjectId.Value] = new ObservableCollection<ChatMessage>(_chatMessages);
            }

            // Загружаем историю чата для выбранного проекта
            _currentProjectId = projectId;

            if (_projectChatHistories.ContainsKey(projectId))
            {
                Console.WriteLine($"  Loading existing chat history. Messages: {_projectChatHistories[projectId].Count}");
                _chatMessages.Clear();
                foreach (var message in _projectChatHistories[projectId])
                {
                    _chatMessages.Add(message);
                }
            }
            else
            {
                Console.WriteLine($"  Creating new empty chat history");
                _chatMessages.Clear();
                _projectChatHistories[projectId] = _chatMessages;
            }

            // Обновляем визуальное выделение проектов
            UpdateProjectSelection(projectId);

            // Обновляем видимость интерфейса
            UpdateChatVisibility();

            // Прокручиваем чат вниз
            ScrollToBottom();

            Console.WriteLine($"✓ Switched to project: {projectId}. Messages: {_chatMessages.Count}");
        }

        private void UpdateProjectSelection(Guid selectedProjectId)
        {
            // Обновляем визуальное выделение всех проектов
            if (ProjectsListControl == null) return;

            foreach (var item in ProjectsListControl.Items)
            {
                var container = ProjectsListControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    var borders = FindVisualChildren<Border>(container);
                    foreach (var border in borders)
                    {
                        if (border.Tag is Guid projectId)
                        {
                            if (projectId == selectedProjectId)
                            {
                                // Выделяем выбранный проект
                                border.BorderThickness = new Thickness(3, 0, 0, 0);
                                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EC4899"));
                                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"));
                            }
                            else
                            {
                                // Снимаем выделение с остальных
                                border.BorderThickness = new Thickness(0);
                                border.BorderBrush = Brushes.Transparent;
                                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF"));
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
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
            Console.WriteLine("=== SendButton_Click STARTED ===");

            // Определяем, какой TextBox используется (стартовая страница или чат)
            TextBox activeTextBox = null;
            Button activeButton = null;

            Console.WriteLine($"InputTextBox: exists={InputTextBox != null}, IsVisible={InputTextBox?.IsVisible}");
            Console.WriteLine($"ChatInputTextBox: exists={ChatInputTextBox != null}, IsVisible={ChatInputTextBox?.IsVisible}");

            if (InputTextBox != null && InputTextBox.IsVisible)
            {
                activeTextBox = InputTextBox;
                activeButton = SendButton;
                Console.WriteLine("✓ Using InputTextBox (welcome screen)");
            }
            else if (ChatInputTextBox != null && ChatInputTextBox.IsVisible)
            {
                activeTextBox = ChatInputTextBox;
                activeButton = ChatSendButton;
                Console.WriteLine("✓ Using ChatInputTextBox (chat mode)");
            }
            else
            {
                Console.WriteLine("✗ ERROR: No visible TextBox found!");
                return;
            }

            Console.WriteLine($"Text in box: '{activeTextBox.Text}'");
            Console.WriteLine($"PlaceholderText: '{PlaceholderText}'");
            Console.WriteLine($"Match placeholder: {activeTextBox.Text == PlaceholderText}");
            Console.WriteLine($"IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(activeTextBox.Text)}");

            if (activeTextBox.Text == PlaceholderText || string.IsNullOrWhiteSpace(activeTextBox.Text))
            {
                Console.WriteLine("✗ Validation FAILED: text is placeholder or empty");
                return;
            }

            var userMessage = activeTextBox.Text;
            Console.WriteLine($"✓ User message accepted: '{userMessage}'");

            // Добавляем сообщение пользователя в чат
            _chatMessages.Add(new ChatMessage(MessageRole.User, userMessage));
            Console.WriteLine($"✓ Added user message. Total messages: {_chatMessages.Count}");

            // Переключаем видимость на чат
            UpdateChatVisibility();

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
            Console.WriteLine($"✓ Added thinking message. Total: {_chatMessages.Count}");

            // Прокручиваем вниз
            ScrollToBottom();

            try
            {
                Console.WriteLine("→ Starting async processing...");

                // Проверяем, выбрано ли несколько агентов
                Console.WriteLine($"Selected agents: {_selectedAgentIds.Count}");

                if (_selectedAgentIds.Count > 1)
                {
                    Console.WriteLine($"→ BRANCH: Multiple agents ({_selectedAgentIds.Count})");
                    // Множественное выполнение агентов параллельно
                    var tasks = new List<Task<(string AgentName, AgentTask Task)>>();

                    foreach (var agentId in _selectedAgentIds)
                    {
                        var agent = _agentManager.GetAgent(agentId);
                        if (agent != null)
                        {
                            Console.WriteLine($"  Adding agent task: {agent.Name}");
                            tasks.Add(Task.Run(async () =>
                            {
                                var task = await _agentExecutor.ExecuteTaskAsync(agent, userMessage);
                                return (agent.Name, task);
                            }));
                        }
                    }

                    Console.WriteLine($"  Waiting for {tasks.Count} agents...");
                    var results = await Task.WhenAll(tasks);
                    Console.WriteLine($"  ✓ Task.WhenAll completed. Got {results.Length} results");

                    // Удаляем индикатор "думает"
                    Console.WriteLine($"  Removing thinking message. Count before: {_chatMessages.Count}");
                    _chatMessages.Remove(thinkingMessage);
                    Console.WriteLine($"  ✓ Thinking message removed. Count after: {_chatMessages.Count}");

                    // Добавляем ответы от каждого агента в чат
                    int processedCount = 0;
                    foreach (var (agentName, task) in results)
                    {
                        processedCount++;
                        Console.WriteLine($"  → Processing result #{processedCount}: Agent={agentName}, Status={task.Status}");

                        string content;
                        if (task.Status == Models.TaskStatus.Completed)
                        {
                            content = task.Response ?? "Нет ответа";
                            Console.WriteLine($"    ✓ Completed. Response length: {content.Length} chars");
                        }
                        else if (task.Status == Models.TaskStatus.Failed)
                        {
                            content = $"❌ Ошибка: {task.ErrorMessage}";
                            Console.WriteLine($"    ✗ Failed: {task.ErrorMessage}");
                        }
                        else
                        {
                            content = "⚠️ Задача была отменена";
                            Console.WriteLine($"    ⚠ Cancelled");
                        }

                        Console.WriteLine($"    Adding message to chat. Current count before add: {_chatMessages.Count}");
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, content, agentName));
                        Console.WriteLine($"    ✓ Message added. New count: {_chatMessages.Count}");
                        ScrollToBottom();
                    }

                    Console.WriteLine($"  ✓ Processed {processedCount} agent responses total");

                    return; // Выходим из метода, сообщения уже добавлены
                }
                // Проверяем, выбран ли один агент
                else if (_selectedAgentIds.Count == 1)
                {
                    Console.WriteLine($"→ BRANCH: Single agent");
                    // Используем выбранного агента из списка
                    var agent = _agentManager.GetAgent(_selectedAgentIds[0]);
                    if (agent != null)
                    {
                        Console.WriteLine($"  Executing agent: {agent.Name}");
                        var task = await _agentExecutor.ExecuteTaskAsync(agent, userMessage);

                        // Удаляем индикатор "думает"
                        Console.WriteLine($"  Removing thinking message. Count before: {_chatMessages.Count}");
                        _chatMessages.Remove(thinkingMessage);
                        Console.WriteLine($"  ✓ Thinking message removed. Count after: {_chatMessages.Count}");

                        string content;
                        if (task.Status == Models.TaskStatus.Completed)
                        {
                            content = task.Response ?? "Нет ответа";
                            Console.WriteLine($"  ✓ Agent completed: {content.Substring(0, Math.Min(50, content.Length))}...");
                        }
                        else if (task.Status == Models.TaskStatus.Failed)
                        {
                            content = $"❌ Ошибка: {task.ErrorMessage}";
                            Console.WriteLine($"  ✗ Agent failed: {task.ErrorMessage}");
                        }
                        else
                        {
                            content = "⚠️ Задача была отменена";
                            Console.WriteLine($"  ⚠ Agent cancelled");
                        }

                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, content, agent.Name));
                        ScrollToBottom();
                        Console.WriteLine($"✓ Response added. Total messages: {_chatMessages.Count}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("  ✗ Agent not found!");
                        // Удаляем индикатор "думает"
                        _chatMessages.Remove(thinkingMessage);
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "❌ Выбранный агент не найден"));
                        ScrollToBottom();
                        return;
                    }
                }
                // Нет выбранных агентов - прямой запрос к AI провайдеру
                else
                {
                    Console.WriteLine($"→ BRANCH: No agents - direct AI call");
                    UpdateAiProvider();

                    if (_currentAiService != null)
                    {
                        Console.WriteLine($"  Calling {_currentAiService.GetServiceName()}...");
                        var response = await _currentAiService.SendMessageAsync(userMessage);

                        // Удаляем индикатор "думает"
                        Console.WriteLine($"  Removing thinking message. Count before: {_chatMessages.Count}");
                        _chatMessages.Remove(thinkingMessage);
                        Console.WriteLine($"  ✓ Thinking message removed. Count after: {_chatMessages.Count}");

                        Console.WriteLine($"  ✓ Got response: {response.Substring(0, Math.Min(50, response.Length))}...");
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, response));
                    }
                    else
                    {
                        Console.WriteLine("  ✗ AI service is null!");
                        // Удаляем индикатор "думает"
                        _chatMessages.Remove(thinkingMessage);
                        _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "❌ AI сервис не инициализирован"));
                    }

                    ScrollToBottom();
                    Console.WriteLine($"✓ Response added. Total messages: {_chatMessages.Count}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("✗ EXCEPTION: OperationCanceledException");
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant, "⚠️ Запрос был отменен"));
                ScrollToBottom();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✗ EXCEPTION: ArgumentException - {ex.Message}");
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant, $"❌ Некорректные данные: {ex.Message}"));
                ScrollToBottom();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"✗ EXCEPTION: HttpRequestException - {ex.Message}");
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Ошибка сети: {ex.Message}\n\nПроверьте подключение к интернету и API ключи."));
                ScrollToBottom();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✗ EXCEPTION: InvalidOperationException - {ex.Message}");
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Ошибка обработки: {ex.Message}"));
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                _chatMessages.Remove(thinkingMessage);
                _chatMessages.Add(new ChatMessage(MessageRole.Assistant,
                    $"❌ Непредвиденная ошибка: {ex.Message}"));
                ScrollToBottom();
            }
            finally
            {
                Console.WriteLine("→ Re-enabling controls...");
                // Восстанавливаем состояние всех контролов
                if (SendButton != null) SendButton.IsEnabled = true;
                if (ChatSendButton != null) ChatSendButton.IsEnabled = true;
                if (InputTextBox != null) InputTextBox.IsEnabled = true;
                if (ChatInputTextBox != null) ChatInputTextBox.IsEnabled = true;
                if (AiProviderComboBox != null) AiProviderComboBox.IsEnabled = true;
                if (AgentCheckboxesPanel != null) AgentCheckboxesPanel.IsEnabled = true;
                Console.WriteLine("=== SendButton_Click FINISHED ===\n");
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

                // Автоматически переключаемся на новый проект
                if (createWindow.CreatedProjectId.HasValue)
                {
                    Console.WriteLine($"New project created: {createWindow.CreatedProjectId.Value}");
                    SwitchToProject(createWindow.CreatedProjectId.Value);
                }
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

        private void ProjectItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Guid projectId)
            {
                Console.WriteLine($"Project clicked: {projectId}");
                SwitchToProject(projectId);
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
