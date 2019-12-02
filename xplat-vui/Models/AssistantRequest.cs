using Alexa.NET.Request;
using Google.Cloud.Dialogflow.V2;
using LineDC.CEK.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace XPlat.VUI.Models
{
    public class AssistantRequest
    {
        public RequestType Type { get; internal set; }
        public string Intent { get; internal set; }
        public Dictionary<string, object> Slots { get; internal set; }
        public Dictionary<string, object> Session { get; internal set; }
        public WebhookRequest OriginalGoogleRequest { get; internal set; }
        public SkillRequest OriginalAlexaRequest { get; internal set; }
        public CEKRequest OriginalClovaRequest { get; internal set; }
        public string UserId { get; internal set; }
        public string DeviceId { get; internal set; }
        public string PreviousAudioId { get; internal set; }
        public Platform CurrentPlatform
        {
            get
            {
                return
                    OriginalGoogleRequest != null ? Platform.GoogleAssistant :
                    OriginalAlexaRequest != null ? Platform.Alexa :
                    OriginalClovaRequest != null ? Platform.Clova :
                    throw new Exception("Request objects are null.");
            }
        }
    }

    public enum RequestType
    {
        LaunchRequest,
        IntentRequest,
        AudioPlayStartedEventRequest,
        AudioPlayPausedOrStoppedEventRequest,
        AudioPlayNearlyFinishedEventRequest,
        AudioPlayFinishedEventRequest,
        UnsupportedRequest
    }
}
