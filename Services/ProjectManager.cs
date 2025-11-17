using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuildHub.Models;

namespace BuildHub.Services
{
    public class ProjectManager
    {
        private static ProjectManager? _instance;
        private readonly string _projectsFilePath;
        private List<Project> _projects;

        private ProjectManager()
        {
            _projectsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "projects.json");
            _projects = LoadProjects();
        }

        public static ProjectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProjectManager();
                }
                return _instance;
            }
        }

        public List<Project> GetAllProjects() => _projects.OrderByDescending(p => p.CreatedAt).ToList();

        public Project? GetProject(Guid id) => _projects.FirstOrDefault(p => p.Id == id);

        public Project CreateProject(string name, string description = "")
        {
            var project = new Project(name, description);
            _projects.Add(project);
            SaveProjects();
            return project;
        }

        public bool UpdateProject(Project project)
        {
            var existingProject = GetProject(project.Id);
            if (existingProject == null)
                return false;

            project.LastModifiedAt = DateTime.Now;
            var index = _projects.IndexOf(existingProject);
            _projects[index] = project;
            SaveProjects();
            return true;
        }

        public bool DeleteProject(Guid id)
        {
            var project = GetProject(id);
            if (project == null)
                return false;

            _projects.Remove(project);
            SaveProjects();
            return true;
        }

        public Dictionary<string, List<Project>> GetProjectsByDateGroup()
        {
            var grouped = new Dictionary<string, List<Project>>();
            var projects = GetAllProjects();

            foreach (var project in projects)
            {
                var group = project.GetDateGroup();
                if (!grouped.ContainsKey(group))
                {
                    grouped[group] = new List<Project>();
                }
                grouped[group].Add(project);
            }

            return grouped;
        }

        private List<Project> LoadProjects()
        {
            try
            {
                if (File.Exists(_projectsFilePath))
                {
                    var json = File.ReadAllText(_projectsFilePath);
                    return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading projects: {ex.Message}");
            }

            return new List<Project>();
        }

        private void SaveProjects()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_projects, options);
                File.WriteAllText(_projectsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving projects: {ex.Message}");
            }
        }
    }
}
