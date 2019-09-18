using Alexa.NET.Response;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf.WellKnownTypes;
using LineDC.CEK.Models;
using System.Collections.Generic;
using System.Linq;

namespace XPlat.VUI.Models
{
    public class AssistantResponse
    {
        private List<OutputObject> OutputObjects { get; set; } = new List<OutputObject>();
        private bool ShouldEndSession { get; set; } = true;
        private string RepromptText { get; set; }
        internal string UserId { get; set; }

        public AssistantResponse Speak(string text, Platform targetPlatform = Platform.All)
        {
            OutputObjects.Add(new OutputObject { TargetPlatform = targetPlatform, Type = OutputType.Text, Value = text });
            return this;
        }

        public AssistantResponse Play(string url, Platform targetPlatform = Platform.All)
        {
            OutputObjects.Add(new OutputObject { TargetPlatform = targetPlatform, Type = OutputType.Url, Value = url });
            return this;
        }

        public void KeepListening(string reprompt = null)
        {
            if (reprompt != null)
            {
                RepromptText = reprompt;
            }
            ShouldEndSession = false;
        }

        public string ToGoogleAssistantResponse()
        {
            var webhookResponse = new WebhookResponse
            {                
                FulfillmentText = GetSsmlResponse(Platform.GoogleAssistant),
                Payload = new Struct
                {
                    Fields =
                    {
                        {
                            "google", Value.ForStruct(new Struct
                            {
                                Fields =
                                {
                                    { "expectUserResponse", Value.ForBool(!ShouldEndSession) },
                                    { "userStorage", Value.ForString($"{{ \"userId\": \"{UserId}\" }}") },
                                    { "resetUserStorage", Value.ForBool(true) }
                                }
                            })
                        },
                    }
                }            
            };
            return webhookResponse.ToString();
        }

        public SkillResponse ToAlexaResponse()
        {
            return new SkillResponse
            {
                Response = new ResponseBody
                {
                    OutputSpeech = new SsmlOutputSpeech
                    {
                        Ssml = GetSsmlResponse(Platform.Alexa)
                    },
                    Reprompt = !string.IsNullOrEmpty(RepromptText)
                        ? new Alexa.NET.Response.Reprompt(RepromptText)
                        : null,
                    ShouldEndSession = ShouldEndSession
                },
                Version = "1.0"
            };
        }

        public CEKResponse ToClovaResponse()
        {
            var response = new CEKResponse();

            foreach (var output in OutputObjects
                .Where(output => output.TargetPlatform == Platform.All || output.TargetPlatform == Platform.Clova))
            {
                if (output.Type == OutputType.Text)
                {
                    response.AddText(output.Value);
                }
                else if (output.Type == OutputType.Url)
                {
                    response.AddUrl(output.Value);
                }
            }

            if (!ShouldEndSession)
            {
                response.KeepListening();
            }

            return response;
        }

        private string GetSsmlResponse(Platform targetPlatform)
        {
            return $@"<speak>{
                string.Join(string.Empty, OutputObjects
                    .Where(output => output.TargetPlatform == Platform.All || output.TargetPlatform == targetPlatform)
                    .Select(output =>
                        output.Type == OutputType.Url
                            ? $"<audio src=\"{output.Value}\" />"
                            : output.Value))
            }</speak>";
        }
    }

    internal class OutputObject
    {
        internal Platform TargetPlatform { get; set; }
        internal OutputType Type { get; set; }
        internal string Value { get; set; }
    }

    internal enum OutputType
    {
        Text,
        Url
    }
}
