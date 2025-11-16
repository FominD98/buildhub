using System.Threading.Tasks;

namespace BuildHub.Services
{
    public interface IAiService
    {
        Task<string> SendMessageAsync(string message);
        string GetServiceName();
    }
}
