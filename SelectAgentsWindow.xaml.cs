using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BuildHub.Services;

namespace BuildHub
{
    public partial class SelectAgentsWindow : Window
    {
        private readonly AgentManager _agentManager;
        private readonly List<Guid> _initialSelection;
        private readonly Dictionary<Guid, CheckBox> _agentCheckBoxes;

        public List<Guid> SelectedAgentIds { get; private set; }

        public SelectAgentsWindow(List<Guid> currentSelection)
        {
            InitializeComponent();
            _agentManager = AgentManager.Instance;
            _initialSelection = new List<Guid>(currentSelection);
            _agentCheckBoxes = new Dictionary<Guid, CheckBox>();
            SelectedAgentIds = new List<Guid>();

            LoadAgents();
            UpdateCount();
        }

        private void LoadAgents()
        {
            var agents = _agentManager.GetAllAgents();

            foreach (var agent in agents)
            {
                var checkBox = new CheckBox
                {
                    Content = agent.Name,
                    Tag = agent.Id,
                    Style = (Style)FindResource("CheckBoxStyle"),
                    IsChecked = _initialSelection.Contains(agent.Id)
                };

                checkBox.Checked += AgentCheckBox_Changed;
                checkBox.Unchecked += AgentCheckBox_Changed;

                _agentCheckBoxes[agent.Id] = checkBox;
                AgentsPanel.Children.Add(checkBox);
            }
        }

        private void AgentCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCount();
        }

        private void UpdateCount()
        {
            var selectedCount = _agentCheckBoxes.Values.Count(cb => cb.IsChecked == true);
            CountTextBlock.Text = $"Выбрано: {selectedCount}";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _agentCheckBoxes.Values)
            {
                checkBox.IsChecked = true;
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _agentCheckBoxes.Values)
            {
                checkBox.IsChecked = false;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAgentIds = _agentCheckBoxes
                .Where(kvp => kvp.Value.IsChecked == true)
                .Select(kvp => kvp.Key)
                .ToList();

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
