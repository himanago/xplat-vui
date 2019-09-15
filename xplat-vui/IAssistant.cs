using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using XPlat.VUI.Models;

namespace XPlat.VUI
{
    public interface IAssistant
    {
        Task<AssistantResponse> RespondAsync(HttpRequest req, Platform targetPlatform, CancellationToken cancellationToken = default);
    }
}
