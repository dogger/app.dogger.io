using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOptionalSingleton<TService, TImplementation>(
            this IServiceCollection services,
            bool condition) 
                where TService : class
                where TImplementation : TService
        {
            services.AddOptionalSingleton<TService>(
                provider => provider.GetRequiredService<TImplementation>(),
                condition);
        }

        public static void AddOptionalSingleton<TService>(
            this IServiceCollection services,
            bool condition) where TService : class
        {
            services.AddOptionalSingleton(
                provider => provider.GetRequiredService<TService>(),
                condition);
        }

        public static void AddOptionalSingleton<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> accessor,
            bool condition) where TService : class
        {
            services.AddOptionalSingleton(
                accessor,
                () => condition);
        }

        public static void AddOptionalSingleton<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> accessor,
            Func<bool> condition) where TService: class
        {
            services.AddSingleton<IOptionalService<TService>>(p =>
                new OptionalService<TService>(
                    condition() ? 
                        accessor(p) : 
                        null));
        }
    }
}
