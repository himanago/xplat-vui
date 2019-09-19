using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LineDC.CEK;
using LineDC.CEK.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XPlat.VUI.Models;

namespace XPlat.VUI
{
    public abstract class AssistantBase : IAssistant
    {
        private static HttpClient _httpClient;
        private static string cert;

        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                    _httpClient = new HttpClient();

                return _httpClient;
            }
        }

        protected AssistantRequest Request { get; private set; }
        protected AssistantResponse Response { get; private set; }

        public async Task<AssistantResponse> RespondAsync(HttpRequest req, Platform targetPlatform, CancellationToken cancellationToken = default)
        {
            Response = new AssistantResponse();

            var bodyContent = ConvertStreamToByteArray(req.Body);

            switch (targetPlatform)
            {
                case Platform.GoogleAssistant:
                    ParseRequestForGoogleAssistant(bodyContent);
                    break;
                case Platform.Alexa:
                    await ParseRequestForAlexa(req, bodyContent);
                    break;
                case Platform.Clova:
                    await ParseRequestForClova(req, bodyContent);
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

            Response.UserId = Request.UserId;
            return Response;
        }

        private byte[] ConvertStreamToByteArray(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[16 * 1024];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private async Task ParseRequestForAlexa(HttpRequest req, byte[] bodyContent)
        {
            var json = Encoding.UTF8.GetString(bodyContent);

            var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);

            await ValidateAlexaRequestAsync(req, skillRequest, json);

            switch (skillRequest.Request)
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

            Request.OriginalAlexaRequest = skillRequest;
            Request.UserId = Request.OriginalAlexaRequest.Session.User.UserId;
        }

        private async Task ValidateAlexaRequestAsync(HttpRequest request, SkillRequest skillRequest, string json)
        {
            if (!request.Headers.TryGetValue("SignatureCertChainUrl", out var signatureChainUrl) ||
                string.IsNullOrWhiteSpace(signatureChainUrl))
            {
                throw new Exception("Empty SignatureCertChainUrl header");
            }

            Uri certUrl;
            try
            {
                certUrl = new Uri(signatureChainUrl);
            }
            catch
            {
                throw new Exception($"SignatureChainUrl not valid: {signatureChainUrl}");
            }

            if (!request.Headers.TryGetValue("Signature", out var signature) ||
                string.IsNullOrWhiteSpace(signature))
            {
                throw new Exception("Empty Signature header");
            }

            if (!RequestVerification.RequestTimestampWithinTolerance(skillRequest) ||
                !(await RequestVerification.Verify(signature, certUrl, json)))
            {
                throw new Exception("Invalid Signature");
            }
        }

        private void ParseRequestForGoogleAssistant(byte[] bodyContent)
        {
            var json = Encoding.UTF8.GetString(bodyContent);

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            var webhookRequest = parser.Parse<WebhookRequest>(json);

            if (webhookRequest.QueryResult.Action == "input.welcome")
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

            Request.OriginalGoogleRequest = webhookRequest;
            Request.UserId = GetGoogleUserId(Request.OriginalGoogleRequest);
        }

        private string GetGoogleUserId(WebhookRequest request)
        {
            string userId = null;
            var intentRequestPayload = request.OriginalDetectIntentRequest?.Payload;

            var userStruct = intentRequestPayload.Fields?["user"];

            string userStorageText = null;
            if ((userStruct?.StructValue?.Fields?.Keys.Contains("userStorage")).GetValueOrDefault(false))
            {
                userStorageText = userStruct?.StructValue?.Fields?["userStorage"]?.StringValue;
            }

            if (!string.IsNullOrWhiteSpace(userStorageText))
            {
                var userStore = JsonConvert.DeserializeObject<GoogleUserStorage>(userStorageText);
                userId = userStore.UserId;
            }
            else
            {
                if ((userStruct?.StructValue?.Fields?.Keys.Contains("userId")).GetValueOrDefault(false))
                {
                    userId = userStruct?.StructValue?.Fields?["userId"]?.StringValue;
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = Guid.NewGuid().ToString("N");
                }
            }
            return userId;
        }

        private async Task ParseRequestForClova(HttpRequest req, byte[] bodyContent)
        {
            var json = Encoding.UTF8.GetString(bodyContent);

            var cekRequest = JsonConvert.DeserializeObject<CEKRequest>(json);

            await ValidateClovaRequestAsync(req.Headers["SignatureCEK"], bodyContent);

            switch (cekRequest.Request.Type)
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
                        Intent = cekRequest.Request.Intent.Name,
                        Slots = cekRequest.Request.Intent.Slots.ToDictionary(s => s.Key, s => (object)s.Value.Value),
                    };
                    break;
            }

            Request.OriginalClovaRequest = cekRequest;
            Request.UserId = Request.OriginalClovaRequest.Session.User.UserId;
        }

        private async Task ValidateClovaRequestAsync(string signatureCEK, byte[] bodyContent)
        {
            if (string.IsNullOrEmpty(signatureCEK))
            {
                throw new Exception("Empty Signature header");
            }

            if (string.IsNullOrEmpty(cert))
            {
                cert = await HttpClient.GetStringAsync("https://clova-cek-requests.line.me/.well-known/signature-public-key.pem");
            }

            var provider = PemKeyUtils.GetRSAProviderFromPemString(cert.Trim());
            if (!provider.VerifyData(bodyContent, Convert.FromBase64String(signatureCEK), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                throw new Exception("Invalid Signature");
            }
        }

        protected virtual Task OnLaunchRequestAsync(Dictionary<string, object> session, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnIntentRequestAsync(string intent, Dictionary<string, object> slots, Dictionary<string, object> session, CancellationToken cancellationToken) => Task.CompletedTask;

    }
}
