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
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dogger.Infrastructure.AspNet.Health;
using Dogger.Infrastructure.Ioc;
using FluffySpoon.AspNet.NGrok;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Octokit;

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
            var registry = new IocRegistry(
                services,
                Configuration);
            registry.Register();

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
                .AddJwtBearer("Auth0", options =>
                {
                    options.Authority = AuthConstants.Auth0Domain;
                    options.Audience = AuthConstants.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier
                    };

                    options.RequireHttpsMetadata = !Environment.IsDevelopment();
                })
                .AddJwtBearer("OnPremises", options =>
                {
                    options.Authority = "https://dogger.io/on-prem";
                    options.Audience = AuthConstants.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters();
                    options.RequireHttpsMetadata = false;
                })
                .AddJwtBearer("Licensing", options =>
                {
                    options.Authority = "https://dogger.io/licensing";
                    options.Audience = AuthConstants.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters();
                    options.RequireHttpsMetadata = false;
                });

            services.AddAuthorization(options =>
            {
                void AddScopePolicy(string permissionName)
                {
                    options.AddPolicy(
                        permissionName,
                        policy => policy.Requirements.Add(
                            new HasScopeRequirement(
                                permissionName)));
                }

                static AuthorizationPolicy BuildSchemePolicy(string scheme)
                {
                    return new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(scheme)
                        .Build();
                }

                void AddSchemePolicy(string scheme)
                {
                    options.AddPolicy(
                        scheme, 
                        BuildSchemePolicy(scheme));
                }

                options.DefaultPolicy = BuildSchemePolicy("Auth0");

                AddSchemePolicy("OnPremises");
                AddSchemePolicy("Licensing");

                AddScopePolicy(Scopes.ReadErrors);
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

            services.AddHttpContextAccessor();

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

        public static void Configure(
            IApplicationBuilder app)
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
        }

        private static bool IsWebhookRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(
                "/api/webhooks",
                StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
