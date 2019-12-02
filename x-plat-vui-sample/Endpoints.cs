using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using XPlat.VUI;
using XPlat.VUI.Models;

namespace XPlat.VUI.Sample
{
    public class Endpoints
    {
        private ILoggableAssistant Assistant { get; }

        public Endpoints(ILoggableAssistant assistant)
        {
            Assistant = assistant;
        }

        [FunctionName(nameof(GoogleEndpoint))]
        public async Task<IActionResult> GoogleEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            Assistant.Logger = log;
            var response = await Assistant.RespondAsync(req, Platform.GoogleAssistant);
            return new OkObjectResult(response.ToGoogleAssistantResponse());
        }

        [FunctionName(nameof(AlexaEndpoint))]
        public async Task<IActionResult> AlexaEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            Assistant.Logger = log;
            var response = await Assistant.RespondAsync(req, Platform.Alexa);
            return new OkObjectResult(response.ToAlexaResponse());
        }

        [FunctionName(nameof(ClovaEndpoint))]
        public async Task<IActionResult> ClovaEndpoint(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            Assistant.Logger = log;
            var response = await Assistant.RespondAsync(req, Platform.Clova);
            return new OkObjectResult(response.ToClovaResponse());
        }

    }
}
