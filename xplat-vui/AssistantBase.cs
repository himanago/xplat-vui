using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LineDC.CEK.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XPlat.VUI.Models;

namespace XPlat.VUI
{
    public abstract class AssistantBase : IAssistant
    {
        protected AssistantRequest Request { get; private set; }
        protected AssistantResponse Response { get; private set; }

        public async Task<AssistantResponse> RespondAsync(HttpRequest req, Platform targetPlatform, CancellationToken cancellationToken = default)
        {
            Response = new AssistantResponse();

            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var json = await reader.ReadToEndAsync();
                switch (targetPlatform)
                {
                    case Platform.GoogleAssistant:
                        ParseRequestForGoogleAssistant(json);
                        break;
                    case Platform.Alexa:
                        ParseRequestForAlexa(json);
                        break;
                    case Platform.Clova:
                        ParseRequestForClova(json);
                        break;
                    default:
                        throw new InvalidOperationException($"targetPlatform:{targetPlatform}");
                }

                switch (Request.Type)
                {
                    case Models.RequestType.LaunchRequest:
                        await OnLaunchRequestAsync(Request.Session, cancellationToken);
                        break;
                    case Models.RequestType.IntentRequest:
                        await OnIntentRequestAsync(Request.Intent, Request.Slots, Request.Session, cancellationToken);
                        break;
                }
            }
            return Response;
        }

        private void ParseRequestForAlexa(string json)
        {
            var alexaRequest = JsonConvert.DeserializeObject<SkillRequest>(json);
            switch (alexaRequest.Request)
            {
                case LaunchRequest lr:
                    Request = new AssistantRequest
                    {
                        Type = Models.RequestType.LaunchRequest
                    };
                    break;
                case IntentRequest ir:
                    Request = new AssistantRequest
                    {
                        Type = Models.RequestType.IntentRequest,
                        Intent = ir.Intent.Name,
                        Slots = ir.Intent.Slots.ToDictionary(s => s.Key, s => (object)s.Value.Value),
                    };
                    break;
            }
        }

        private void ParseRequestForGoogleAssistant(string json)
        {
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            var webhookRequest = parser.Parse<WebhookRequest>(json);

            if (webhookRequest.QueryResult.Intent.DisplayName == "Default Welcome Intent")
            {
                Request = new AssistantRequest
                {
                    Type = Models.RequestType.LaunchRequest
                };
            }
            else
            {
                Request = new AssistantRequest
                {
                    Type = Models.RequestType.IntentRequest,
                    Intent = webhookRequest.QueryResult.Intent.DisplayName,
                    Slots = webhookRequest.QueryResult.Parameters.Fields.ToDictionary(f => f.Key, f =>
                        f.Value.KindCase switch {
                            Value.KindOneofCase.StringValue => f.Value.StringValue,
                            Value.KindOneofCase.NumberValue => f.Value.NumberValue,
                            Value.KindOneofCase.BoolValue => (object)f.Value.BoolValue,
                            Value.KindOneofCase.StructValue => f.Value.StructValue,
                            Value.KindOneofCase.ListValue => f.Value.ListValue.Values.Select(v =>
                                v.KindCase switch {
                                    Value.KindOneofCase.StringValue => v.StringValue,
                                    Value.KindOneofCase.NumberValue => v.NumberValue,
                                    Value.KindOneofCase.BoolValue => (object)v.BoolValue,
                                    Value.KindOneofCase.StructValue => v.StructValue,
                                    _ => v.NullValue
                                }).ToList(),
                            _ => f.Value.NullValue
                        })
                };
            }
        }

        private void ParseRequestForClova(string json)
        {
            var clovaRequest = JsonConvert.DeserializeObject<CEKRequest>(json);
            switch (clovaRequest.Request.Type)
            {
                case LineDC.CEK.Models.RequestType.LaunchRequest:
                    Request = new AssistantRequest
                    {
                        Type = Models.RequestType.LaunchRequest
                    };
                    break;
                case LineDC.CEK.Models.RequestType.IntentRequest:
                    Request = new AssistantRequest
                    {
                        Type = Models.RequestType.IntentRequest,
                        Intent = clovaRequest.Request.Intent.Name,
                        Slots = clovaRequest.Request.Intent.Slots.ToDictionary(s => s.Key, s => (object)s.Value.Value),
                    };
                    break;
            }
        }

        protected virtual Task OnLaunchRequestAsync(Dictionary<string, object> session, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnIntentRequestAsync(string intent, Dictionary<string, object> slots, Dictionary<string, object> session, CancellationToken cancellationToken) => Task.CompletedTask;

    }
}
