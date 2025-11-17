using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BuildHub.Models;

namespace BuildHub.Services
{
    public class AgentManager
    {
        private static AgentManager? _instance;
        private readonly string _agentsFilePath;
        private List<Agent> _agents;

        private AgentManager()
        {
            _agentsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "agents.json");
            _agents = LoadAgents();
        }

        public static AgentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AgentManager();
                }
                return _instance;
            }
        }

        public List<Agent> GetAllAgents() => _agents;

        public Agent? GetAgent(Guid id) => _agents.FirstOrDefault(a => a.Id == id);

        public Agent CreateAgent(string name, string description, string systemPrompt, AiProvider provider)
        {
            var agent = new Agent
            {
                Name = name,
                Description = description,
                SystemPrompt = systemPrompt,
                Provider = provider
            };

            _agents.Add(agent);
            SaveAgents();
            return agent;
        }

        public bool UpdateAgent(Agent agent)
        {
            var existingAgent = GetAgent(agent.Id);
            if (existingAgent == null)
                return false;

            var index = _agents.IndexOf(existingAgent);
            _agents[index] = agent;
            SaveAgents();
            return true;
        }

        public bool DeleteAgent(Guid id)
        {
            var agent = GetAgent(id);
            if (agent == null)
                return false;

            _agents.Remove(agent);
            SaveAgents();
            return true;
        }

        public void AddTaskToAgent(Guid agentId, AgentTask task)
        {
            var agent = GetAgent(agentId);
            if (agent != null)
            {
                agent.TaskHistory.Add(task);
                agent.LastUsedAt = DateTime.Now;
                SaveAgents();
            }
        }

        private List<Agent> LoadAgents()
        {
            try
            {
                if (File.Exists(_agentsFilePath))
                {
                    var json = File.ReadAllText(_agentsFilePath);
                    return JsonSerializer.Deserialize<List<Agent>>(json) ?? new List<Agent>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading agents: {ex.Message}");
            }

            return new List<Agent>();
        }

        private void SaveAgents()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_agents, options);
                File.WriteAllText(_agentsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving agents: {ex.Message}");
            }
        }
    }
}
