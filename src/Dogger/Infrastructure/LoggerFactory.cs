using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Destructurama;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Slack.Core;

namespace Dogger.Infrastructure
{
    public static class LoggerFactory
    {
        private static LoggerConfiguration CreateBaseLoggingConfiguration()
        {
            return new LoggerConfiguration()
                .Destructure.UsingAttributes()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Information);
        }

        public static ILogger BuildDogfeedLogger()
        {
            return CreateBaseLoggingConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        public static ILogger BuildWebApplicationLogger(IConfigurationRoot configuration)
        {
            if (Debugger.IsAttached)
                return BuildDogfeedLogger();

            SelfLog.Enable(Console.Error);

            var slackWebhookUrl = configuration["Slack:IncomingUrl"];
            return CreateBaseLoggingConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Slack(slackWebhookUrl, restrictedToMinimumLevel: LogEventLevel.Error)
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("https://elasticsearch:9200"))
                {
                    FailureCallback = e => Console.WriteLine($"Unable to log message with template {e.MessageTemplate}"),
                    EmitEventFailure =
                        EmitEventFailureHandling.WriteToSelfLog |
                        EmitEventFailureHandling.WriteToFailureSink |
                        EmitEventFailureHandling.RaiseCallback,
                    FailureSink = new SlackSink(
                        slackWebhookUrl,
                        default(SlackSink.RenderMessageMethod),
                        default,
                        default,
                        default),
                    NumberOfReplicas = 0,
                    NumberOfShards = 1,
                    MinimumLogEventLevel = LogEventLevel.Verbose,
                    DetectElasticsearchVersion = false,
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                    RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway,
                    ModifyConnectionSettings = x => x
                        .BasicAuthentication("elastic", "elastic")
                        .RequestTimeout(TimeSpan.FromMinutes(1))
                        .MaxRetryTimeout(TimeSpan.FromHours(1))
                        .MaximumRetries(1000)
                        .ThrowExceptions()
                        .ServerCertificateValidationCallback((a, b, c, d) => true),
                    ConnectionTimeout = TimeSpan.FromMinutes(15)
                })
                .CreateLogger();
        }
    }
}
