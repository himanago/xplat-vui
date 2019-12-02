using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XPlat.VUI;

namespace XPlat.VUI.Sample
{
    public class MyAssistant : AssistantBase, ILoggableAssistant
    {
        public ILogger Logger { get; set; }

        protected override Task OnLaunchRequestAsync(Dictionary<string, object> session, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Launch request");

            Response
                .Speak("お元気ですか？")
                .Break(5)
                .Speak("元気かどうか教えてください。")
                .KeepListening("元気かどうか教えてください。");
           return Task.CompletedTask;
        }

        protected override Task OnIntentRequestAsync(string intent, Dictionary<string, object> slots, Dictionary<string, object> session, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Intent request");

            switch (intent)
            {
                case string s when s.EndsWith("YesIntent"):
                    Response.Speak("よかったですね。");
                    break;

                case string s when s.EndsWith("NoIntent"):
                    Response
                        .Speak("元気を出してくださいね。")
                        .PlayWithAudioPlayer(
                            "sample", "https://xplatvuisamplesaudiofile.blob.core.windows.net/audio/yukinomai.mp3",
                            "Sample Title", "Sample Subtitle").
                            KeepListening();
                    break;

                default:
                    Response.Speak("Error");
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
