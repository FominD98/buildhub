using System.Windows;
using System.Windows.Input;
using BuildHub.Services;

namespace BuildHub
{
    public partial class CreateAgentWindow : Window
    {
        private readonly AgentManager _agentManager;

        public CreateAgentWindow()
        {
            InitializeComponent();
            _agentManager = AgentManager.Instance;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название агента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SystemPromptTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите системный промпт для агента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Определяем провайдера
            var provider = ProviderComboBox.SelectedIndex switch
            {
                0 => AiProvider.ChatGPT,
                1 => AiProvider.DeepSeek,
                2 => AiProvider.YandexGPT,
                _ => AiProvider.ChatGPT
            };

            // Создаем агента
            _agentManager.CreateAgent(
                NameTextBox.Text.Trim(),
                DescriptionTextBox.Text.Trim(),
                SystemPromptTextBox.Text.Trim(),
                provider
            );

            this.DialogResult = true;
            this.Close();
        }
    }
}
