using System;
using System.Windows;
using System.Windows.Input;
using BuildHub.Services;

namespace BuildHub
{
    public partial class CreateProjectWindow : Window
    {
        private readonly ProjectManager _projectManager;
        public Guid? CreatedProjectId { get; private set; }

        public CreateProjectWindow()
        {
            InitializeComponent();
            _projectManager = ProjectManager.Instance;
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
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название проекта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var createdProject = _projectManager.CreateProject(NameTextBox.Text.Trim(), DescriptionTextBox.Text.Trim());
            CreatedProjectId = createdProject.Id;

            this.DialogResult = true;
            this.Close();
        }
    }
}
