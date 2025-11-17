using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BuildHub.Models;
using BuildHub.Services;

namespace BuildHub
{
    public partial class AgentWorkspaceWindow : Window
    {
        private readonly Agent _agent;
        private readonly AgentExecutor _agentExecutor;
        private readonly AgentManager _agentManager;

        public AgentWorkspaceWindow(Agent agent)
        {
            InitializeComponent();
            _agent = agent;
            _agentExecutor = new AgentExecutor();
            _agentManager = AgentManager.Instance;

            LoadAgentInfo();
            LoadTaskHistory();
        }

        private void LoadAgentInfo()
        {
            AgentNameTextBlock.Text = _agent.Name;
            AgentDescriptionTextBlock.Text = _agent.Description;
            SystemPromptTextBlock.Text = _agent.SystemPrompt;
            ProviderTextBlock.Text = _agent.Provider.ToString();
        }

        private void LoadTaskHistory()
        {
            var tasks = _agent.TaskHistory.OrderByDescending(t => t.CreatedAt).Take(10).ToList();
            TaskHistoryControl.ItemsSource = tasks;
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

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter для отправки
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var userPrompt = UserInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                MessageBox.Show("Пожалуйста, введите задачу для агента", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Добавляем сообщение пользователя
            AddMessageToConversation(userPrompt, true);

            // Очищаем поле ввода и блокируем интерфейс
            UserInputTextBox.Text = string.Empty;
            UserInputTextBox.IsEnabled = false;
            SendButton.IsEnabled = false;

            try
            {
                // Добавляем индикатор загрузки
                var loadingBorder = AddLoadingIndicator();

                // Выполняем задачу
                var task = await _agentExecutor.ExecuteTaskAsync(_agent, userPrompt);

                // Удаляем индикатор загрузки
                ConversationPanel.Children.Remove(loadingBorder);

                // Добавляем ответ агента
                if (task.Status == TaskStatus.Completed && !string.IsNullOrEmpty(task.Response))
                {
                    AddMessageToConversation(task.Response, false);
                }
                else if (task.Status == TaskStatus.Failed)
                {
                    AddMessageToConversation($"Ошибка: {task.ErrorMessage}", false, true);
                }
                else if (task.Status == TaskStatus.Cancelled)
                {
                    AddMessageToConversation("Задача была отменена", false, true);
                }

                // Обновляем историю
                LoadTaskHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении задачи:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем интерфейс
                UserInputTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                UserInputTextBox.Focus();
            }
        }

        private void AddMessageToConversation(string message, bool isUser, bool isError = false)
        {
            var border = new Border
            {
                Background = isUser
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15FFFFFF")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 700
            };

            var stackPanel = new StackPanel();

            var labelText = new TextBlock
            {
                Text = isUser ? "Вы" : _agent.Name,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var messageText = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = isError
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(labelText);
            stackPanel.Children.Add(messageText);
            border.Child = stackPanel;

            ConversationPanel.Children.Add(border);

            // Прокручиваем вниз
            Dispatcher.InvokeAsync(() =>
            {
                var scrollViewer = FindScrollViewer(ConversationPanel);
                scrollViewer?.ScrollToEnd();
            });
        }

        private Border AddLoadingIndicator()
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15FFFFFF")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 700
            };

            var stackPanel = new StackPanel();

            var labelText = new TextBlock
            {
                Text = _agent.Name,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var messageText = new TextBlock
            {
                Text = "Обрабатываю запрос...",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(labelText);
            stackPanel.Children.Add(messageText);
            border.Child = stackPanel;

            ConversationPanel.Children.Add(border);

            // Прокручиваем вниз
            Dispatcher.InvokeAsync(() =>
            {
                var scrollViewer = FindScrollViewer(ConversationPanel);
                scrollViewer?.ScrollToEnd();
            });

            return border;
        }

        private ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            if (parent == null) return null;

            var parentObj = VisualTreeHelper.GetParent(parent);
            if (parentObj == null) return null;

            if (parentObj is ScrollViewer scrollViewer)
                return scrollViewer;

            return FindScrollViewer(parentObj);
        }
    }
}
