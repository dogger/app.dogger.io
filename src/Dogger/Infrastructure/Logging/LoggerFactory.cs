using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Destructurama;
using Dogger.Infrastructure.AspNet.Options;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Slack.Core;

namespace Dogger.Infrastructure.Logging
{
    [ExcludeFromCodeCoverage]
    public static class LoggerFactory
    {
        private static LoggerConfiguration CreateBaseLoggingConfiguration()
        {
            return new LoggerConfiguration()
                .Destructure.UsingAttributes()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Information)
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"));
        }

        public static LoggerConfiguration BuildDogfeedLogConfiguration()
        {
            return CreateBaseLoggingConfiguration()
                .WriteTo.Console();
        }

        public static ILogger BuildDogfeedLogger()
        {
            return BuildDogfeedLogConfiguration()
                .CreateLogger();
        }

        public static ILogger BuildWebApplicationLogger(IConfiguration configuration)
        {
            return BuildWebApplicationLogConfiguration(configuration)
                .CreateLogger();
        }

        public static LoggerConfiguration BuildWebApplicationLogConfiguration(IConfiguration configuration)
        {
            if (Debugger.IsAttached)
                return BuildDogfeedLogConfiguration();

            SelfLog.Enable(Console.Error);

            var loggerConfiguration = CreateBaseLoggingConfiguration()
                .Enrich.FromLogContext()
                .Filter.ByExcluding(Matching.FromSource("Elastic.Apm"))
                .WriteTo.Console();

            var loggingOptions = configuration.GetSection<LoggingOptions>();

            var elasticsearchLoggingUrl = loggingOptions?.ElasticsearchLoggingUrl;
            if (!string.IsNullOrWhiteSpace(elasticsearchLoggingUrl))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Sink(
                    new NonDisposableSinkProxy(
                        new ElasticsearchSink(
                            new ElasticsearchSinkOptions(new Uri(elasticsearchLoggingUrl))
                            {
                                FailureCallback = e => Console.WriteLine($"Unable to log message {e.MessageTemplate}"),
                                EmitEventFailure =
                                    EmitEventFailureHandling.WriteToSelfLog |
                                    EmitEventFailureHandling.RaiseCallback,
                                NumberOfReplicas = 0,
                                NumberOfShards = 1,
                                BatchPostingLimit = 10,
                                BufferFileCountLimit = 0,
                                MinimumLogEventLevel = LogEventLevel.Verbose,
                                DetectElasticsearchVersion = false,
                                AutoRegisterTemplate = true,
                                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                                RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway,
                                ModifyConnectionSettings = x => x
                                    .BasicAuthentication("elastic", "elastic")
                                    .ServerCertificateValidationCallback((a, b, c, d) => true)
                            })));
            }

            var slackWebhookUrl = configuration.GetSection<SlackOptions>()?.IncomingUrl;
            if (!string.IsNullOrWhiteSpace(slackWebhookUrl))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Slack(slackWebhookUrl, restrictedToMinimumLevel: LogEventLevel.Error);
            }

            return loggerConfiguration;
        }
    }
}
