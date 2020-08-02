using System;
using System.Collections.Generic;
using System.Text;
using Dogger.Setup.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DoggerIocRegistry = Dogger.Infrastructure.IocRegistry;

namespace Dogger.Setup.Infrastructure
{
    public static class IocRegistry
    {
        public static void Register(
            IServiceCollection services,
            IConfiguration configuration)
        {
            DoggerIocRegistry.Register(
                services, 
                configuration);

            services.AddMediatR(typeof(IocRegistry).Assembly);

            ConfigureOptions(
                services,
                configuration);

            ConfigureDogfeeding(services);
        }

        private static void ConfigureOptions(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<DogfeedOptions>(configuration);
        }

        private static void ConfigureDogfeeding(
            IServiceCollection services)
        {
            services.AddTransient<IDogfeedService, DogfeedService>();
        }
    }
}
