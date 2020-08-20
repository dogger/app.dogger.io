using System;
using Dogger.Infrastructure.Ioc;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOptionalSingleton<TImplementation>(
            this IServiceCollection services,
            bool condition)
            where TImplementation : class
        {
            services.AddOptionalSingleton<TImplementation>(
                () => condition);
        }

        public static void AddOptionalSingleton<TImplementation>(
            this IServiceCollection services,
            Func<bool> condition)
            where TImplementation : class
        {
            services.AddSingleton<TImplementation>();
            services.AddSingleton<IOptionalService<TImplementation>>(p =>
                new OptionalService<TImplementation>(
                    condition() ?
                        p.GetRequiredService<TImplementation>() :
                        null));
        }

        public static void AddOptionalSingleton<TService, TImplementation>(
            this IServiceCollection services,
            bool condition)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddSingleton<TImplementation>();
            services.AddOptionalSingleton<TService, TImplementation>(
                provider => provider.GetRequiredService<TImplementation>(),
                () => condition);
        }

        public static void AddOptionalSingleton<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, TImplementation> accessor,
            bool condition)
                where TService : class
                where TImplementation : class, TService
        {
            services.AddOptionalSingleton<TService, TImplementation>(
                accessor,
                () => condition);
        }

        public static void AddOptionalSingleton<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, TImplementation> accessor,
            Func<bool> condition)
                where TService : class
                where TImplementation : class, TService
        {
            services.AddSingleton<TService>(accessor);
            services.AddSingleton<IOptionalService<TService>>(p =>
                new OptionalService<TService>(
                    condition() ?
                        accessor(p) :
                        null));
        }
    }
}
