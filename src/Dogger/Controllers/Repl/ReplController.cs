using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dogger.Controllers.Repl
{
    [ApiController]
    [Route("api/repl")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ExcludeFromCodeCoverage]
    public class ReplController : ControllerBase
    {
        private readonly IFlurlClient client;

        public ReplController(
            IFlurlClientFactory flurlClientFactory,
            IHostEnvironment hostEnvironment)
        {
            var url = GetReplUrl(hostEnvironment);
            this.client = flurlClientFactory.Get(url).Configure(settings =>
            {
                settings.JsonSerializer = new NewtonsoftJsonSerializer(
                    new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
            });
        }

        private static string GetReplUrl(IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsDevelopment() ? 
                "http://localhost:3002/" : 
                "http://dogger-cli-repl:3001/";
        }

        [HttpPost]
        [Route("authenticated")]
        public async Task PostCommandAuthenticated([FromBody] CommandRequest request)
        {
            try
            {
                var replCommand = $"{request.Command} --dogger-token {GetJwtToken()}";
                var response = await this.client
                    .Request()
                    .PostJsonAsync(new CommandRequest()
                    {
                        Command = replCommand
                    });
                await TransferResponseAsync(response);
            }
            catch (FlurlHttpException ex)
            {
                await TransferResponseAsync(ex.Call.Response);
            }
        }

        [HttpPost]
        [Route("anonymous")]
        [AllowAnonymous]
        public async Task PostCommandAnonymous([FromBody] CommandRequest request)
        {
            try
            {
                var response = await this.client
                    .Request()
                    .PostJsonAsync(new CommandRequest()
                    {
                        Command = request.Command
                    });
                await TransferResponseAsync(response);
            }
            catch (FlurlHttpException ex)
            {
                await TransferResponseAsync(ex.Call.Response);
            }
        }

        private string GetJwtToken()
        {
            var headerValues = this.Request.Headers["Authorization"];
            var headerValue = headerValues.First();

            const string bearerPrefix = "Bearer ";
            if (headerValue.StartsWith(bearerPrefix, StringComparison.InvariantCulture))
                headerValue = headerValue.Substring(bearerPrefix.Length);

            return headerValue;
        }

        private async Task TransferResponseAsync(HttpResponseMessage? response)
        {
            if (response == null)
            {
                this.Response.StatusCode = StatusCodes.Status410Gone;
                return;
            }

            this.Response.StatusCode = (int) response.StatusCode;

            var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(this.Response.Body);
        }
    }

}
