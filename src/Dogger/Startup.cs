using Dogger.Infrastructure;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dogger.Infrastructure.AspNet.Health;
using FluffySpoon.AspNet.NGrok;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;

namespace Dogger
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Startup
    {
        public Startup(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            this.Configuration = configuration;
            this.Environment = environment;

            IdentityModelEventSource.ShowPII = environment.IsDevelopment();
        }

        public IConfiguration Configuration
        {
            get;
        }

        public IHostEnvironment Environment
        {
            get;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            IocRegistry.Register(
                services,
                Configuration);

            ConfigureNGrok(services);
            ConfigureAspNetCore(services);
            ConfigureSwagger(services);
            ConfigureAuthentication(services);
        }

        private static void ConfigureNGrok(IServiceCollection services)
        {
            services.AddNGrok();
        }

        private void ConfigureAuthentication(IServiceCollection services)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = Constants.Domain;
                    options.Audience = Constants.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier
                    };

                    options.RequireHttpsMetadata = !Environment.IsDevelopment();
                });

            services.AddAuthorization(options =>
            {
                void AddPolicy(string permissionName)
                {
                    options.AddPolicy(
                        permissionName,
                        policy => policy.Requirements.Add(
                            new HasScopeRequirement(
                                permissionName)));
                }

                AddPolicy(Scopes.ReadErrors);
            });

            services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
        }

        private static void ConfigureAspNetCore(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var mvcBuilder = services
                .AddMvcCore()
                .AddJsonOptions(x =>
                {
                    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                })
                .AddViews()
                .AddRazorViewEngine()
                .AddApplicationPart(typeof(Startup).Assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddApiExplorer();

            if (Debugger.IsAttached)
                mvcBuilder.AddRazorRuntimeCompilation();

            services.AddHealthChecks();

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "wwwroot/build";
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder => builder
                        .WithOrigins("https://dogger.io")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            services.AddHostedService<InstanceCleanupJob>();
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "General",
                    Version = "v1"
                });

                c.TagActionsBy(x => new[] { "General" });
            });
        }

        private static void AddCacheHeaders(StaticFileResponseContext context, TimeSpan duration)
        {
            context.Context.Response.Headers.Append(
                "Cache-Control",
                "public,max-age=" + (int)duration.TotalSeconds);

            context.Context.Response.Headers.Append(
                "Expires",
                DateTime.UtcNow
                    .Add(duration)
                    .ToString("R", CultureInfo.InvariantCulture));
        }

        public static void Configure(
            IApplicationBuilder app,
            IHostEnvironment environment)
        {
            app.UseForwardedHeaders();

            app.UseNGrokAutomaticUrlDetection();

            app.UseResponseCompression();

            app.UseExceptionHandler("/errors/details");

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();

            app.UseWhen(
                IsWebhookRequest,
                webhookApp => webhookApp.Use(async (context, next) =>
                {
                    context.Request.EnableBuffering();
                    await next();
                }));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResponseWriter = async (context, report) =>
                    {
                        var result = JsonSerializer.Serialize(new HealthResult
                        {
                            Status = report.Status.ToString(),
                            Duration = report.TotalDuration,
                            Information = report.Entries
                                .Select(e => new HealthInformation
                                {
                                    Key = e.Key,
                                    Description = e.Value.Description,
                                    Duration = e.Value.Duration,
                                    Status = Enum.GetName(typeof(HealthStatus),
                                        e.Value.Status),
                                    Error = e.Value.Exception?.Message
                                })
                                .ToList()

                        });

                        context.Response.ContentType = MediaTypeNames.Application.Json;

                        await context.Response.WriteAsync(result);
                    }
                });

                endpoints
                    .MapControllers()
                    .RequireAuthorization();
            });

            if (ShouldUseSpaServices(environment))
            {
                app.UseWhen(
                    IsNonApiGetRequest,
                    a => a.UseSpaStaticFiles());

                app.UseWhen(
                    IsNonApiGetRequest, a => a
                        .UseSpa(spa =>
                        {
                            spa.Options.SourcePath = "wwwroot";
                            spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
                        }));
            }
            else if (!EnvironmentHelper.IsRunningInTest)
            {
                app.UseWhen(IsNonApiGetRequest, a =>
                {
                    app.UseDefaultFiles(new DefaultFilesOptions()
                    {
                        FileProvider = GetPublicRootFileProvider()
                    });
                    a.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = GetPublicRootFileProvider(),
                        OnPrepareResponse = staticFileResponseContext =>
                        {
                            AddCacheHeaders(staticFileResponseContext, TimeSpan.FromDays(365));
                        }
                    });
                });
            }
        }

        private static PhysicalFileProvider GetPublicRootFileProvider()
        {
            return new PhysicalFileProvider(Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "public"));
        }

        private static bool ShouldUseSpaServices(IHostEnvironment environment)
        {
            return environment.IsDevelopment() && !EnvironmentHelper.IsRunningInTest;
        }

        private static bool IsWebhookRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(
                "/api/webhooks",
                StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsNonApiGetRequest(HttpContext httpContext)
        {
            return
                httpContext.Request.Method == HttpMethods.Get &&
                !httpContext.Request.Path.StartsWithSegments("/api", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
