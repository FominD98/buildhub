using System;
using System.Windows;
using System.Windows.Input;
using BuildHub.Services;
using BuildHub.Models;

namespace BuildHub
{
    public partial class CreateAgentWindow : Window
    {
        private readonly AgentManager _agentManager;
        private Agent? _editingAgent;
        private bool _isEditMode;

        public CreateAgentWindow()
        {
            InitializeComponent();
            _agentManager = AgentManager.Instance;
            _isEditMode = false;
        }

        public CreateAgentWindow(Agent agent) : this()
        {
            _editingAgent = agent;
            _isEditMode = true;
            LoadAgentData();
        }

        private void LoadAgentData()
        {
            if (_editingAgent == null) return;

            // Заполняем поля данными агента
            NameTextBox.Text = _editingAgent.Name;
            DescriptionTextBox.Text = _editingAgent.Description;
            SystemPromptTextBox.Text = _editingAgent.SystemPrompt;

            // Устанавливаем провайдера
            ProviderComboBox.SelectedIndex = _editingAgent.Provider switch
            {
                AiProvider.ChatGPT => 0,
                AiProvider.DeepSeek => 1,
                AiProvider.YandexGPT => 2,
                _ => 0
            };

            // Меняем заголовок окна и кнопки
            WindowTitle.Text = "Редактирование агента";
            //CreateButton.Content = "Сохранить";
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

            if (_isEditMode && _editingAgent != null)
            {
                // Режим редактирования - обновляем существующего агента
                _editingAgent.Name = NameTextBox.Text.Trim();
                _editingAgent.Description = DescriptionTextBox.Text.Trim();
                _editingAgent.SystemPrompt = SystemPromptTextBox.Text.Trim();
                _editingAgent.Provider = provider;

                _agentManager.UpdateAgent(_editingAgent);
            }
            else
            {
                // Режим создания - создаем нового агента
                _agentManager.CreateAgent(
                    NameTextBox.Text.Trim(),
                    DescriptionTextBox.Text.Trim(),
                    SystemPromptTextBox.Text.Trim(),
                    provider
                );
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
