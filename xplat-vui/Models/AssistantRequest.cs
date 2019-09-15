using System;
using System.Collections.Generic;
using System.Text;

namespace XPlat.VUI.Models
{
    public class AssistantRequest
    {
        public RequestType Type { get; set; }
        public string Intent { get; set; }
        public Dictionary<string, object> Slots { get; set; }
        public Dictionary<string, object> Session { get; set; }
    }

    public enum RequestType
    {
        LaunchRequest,
        IntentRequest
    }
}
