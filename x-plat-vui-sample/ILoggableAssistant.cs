using Microsoft.Extensions.Logging;
using XPlat.VUI;

namespace XPlat.VUI.Sample
{
    public interface ILoggableAssistant : IAssistant
    {
        ILogger Logger { get; set; }
    }
}
