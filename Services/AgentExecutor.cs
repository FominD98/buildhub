using System;
using System.Threading;
using System.Threading.Tasks;
using BuildHub.Models;
using TaskStatus = BuildHub.Models.TaskStatus;

namespace BuildHub.Services
{
    public class AgentExecutor
    {
        private readonly AiServiceFactory _aiServiceFactory;
        private readonly AgentManager _agentManager;

        public AgentExecutor()
        {
            var configService = ConfigurationService.Instance;
            var config = configService.GetApiConfig();
            _aiServiceFactory = new AiServiceFactory(config);
            _agentManager = AgentManager.Instance;
        }

        public async Task<AgentTask> ExecuteTaskAsync(Guid agentId, string userPrompt, CancellationToken cancellationToken = default)
        {
            var agent = _agentManager.GetAgent(agentId);
            if (agent == null)
            {
                throw new InvalidOperationException($"Agent with ID {agentId} not found");
            }

            var task = new AgentTask(agentId, userPrompt)
            {
                Status = TaskStatus.Running,
                StartedAt = DateTime.Now
            };

            try
            {
                // Создаем AI сервис для агента
                var aiService = _aiServiceFactory.CreateService(agent.Provider);

                // Формируем полный промпт: системный промпт агента + промпт пользователя
                var fullPrompt = string.IsNullOrWhiteSpace(agent.SystemPrompt)
                    ? userPrompt
                    : $"{agent.SystemPrompt}\n\nUser Request: {userPrompt}";

                // Выполняем запрос
                var response = await aiService.SendMessageAsync(fullPrompt, cancellationToken);

                // Обновляем задачу
                task.Response = response;
                task.Status = TaskStatus.Completed;
                task.CompletedAt = DateTime.Now;
                task.Duration = task.CompletedAt - task.StartedAt;
            }
            catch (OperationCanceledException)
            {
                task.Status = TaskStatus.Cancelled;
                task.ErrorMessage = "Task was cancelled";
                task.CompletedAt = DateTime.Now;
                task.Duration = task.CompletedAt - task.StartedAt;
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.CompletedAt = DateTime.Now;
                task.Duration = task.CompletedAt - task.StartedAt;
            }

            // Сохраняем задачу в истории агента
            if (agent.Settings.SaveHistory)
            {
                _agentManager.AddTaskToAgent(agentId, task);
            }

            return task;
        }

        public async Task<AgentTask> ExecuteTaskAsync(Agent agent, string userPrompt, CancellationToken cancellationToken = default)
        {
            return await ExecuteTaskAsync(agent.Id, userPrompt, cancellationToken);
        }
    }
}
