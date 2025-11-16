using System.Threading;
using System.Threading.Tasks;

namespace BuildHub.Services
{
    public interface IAiService
    {
        Task<string> SendMessageAsync(string message);
        Task<string> SendMessageAsync(string message, CancellationToken cancellationToken);
        string GetServiceName();
    }
}
