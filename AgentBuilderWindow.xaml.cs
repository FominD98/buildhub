using System.Windows;
using System.Windows.Input;
using BuildHub.Services;
using BuildHub.Models;

namespace BuildHub
{
    public partial class AgentBuilderWindow : Window
    {
        private readonly AgentManager _agentManager;

        public AgentBuilderWindow()
        {
            InitializeComponent();
            _agentManager = AgentManager.Instance;
            LoadAgents();
        }

        private void LoadAgents()
        {
            var agents = _agentManager.GetAllAgents();
            // Очищаем и обновляем ItemsSource для принудительного обновления UI
            AgentsListControl.ItemsSource = null;
            AgentsListControl.ItemsSource = agents;
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

        private void CreateAgentButton_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateAgentWindow();
            if (createWindow.ShowDialog() == true)
            {
                LoadAgents();
            }
        }

        private void AgentCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Agent agent)
            {
                var workspaceWindow = new AgentWorkspaceWindow(agent);
                workspaceWindow.Show();
            }
        }

        private void EditAgentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement button && button.Tag is Agent agent)
            {
                var editWindow = new CreateAgentWindow(agent);
                if (editWindow.ShowDialog() == true)
                {
                    LoadAgents();
                }
                // Останавливаем событие, чтобы не открылся workspace
                if (e is MouseButtonEventArgs mouseEvent)
                {
                    mouseEvent.Handled = true;
                }
            }
        }

        private void DeleteAgentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement button && button.Tag is Agent agent)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить агента '{agent.Name}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _agentManager.DeleteAgent(agent.Id);
                    LoadAgents();
                }

                // Останавливаем событие, чтобы не открылся workspace
                if (e is MouseButtonEventArgs mouseEvent)
                {
                    mouseEvent.Handled = true;
                }
            }
        }
    }
}
