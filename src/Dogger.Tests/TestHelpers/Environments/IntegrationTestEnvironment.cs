﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Tests.TestHelpers.Environments
{

    [ExcludeFromCodeCoverage]
    public abstract class IntegrationTestEnvironment<TOptions> : IAsyncDisposable
        where TOptions : class, new()
    {
        private readonly IIntegrationTestEntrypoint entrypoint;

        public IServiceProvider ServiceProvider { get; }

        public IMediator Mediator => this.ServiceProvider.GetRequiredService<Mediator>();
        public DataContext DataContext => this.ServiceProvider.GetRequiredService<DataContext>();
        public IConfiguration Configuration => this.ServiceProvider.GetRequiredService<IConfiguration>();
        public StripeEnvironmentContext Stripe => new StripeEnvironmentContext(this.ServiceProvider);

        protected abstract IIntegrationTestEntrypoint GetEntrypoint(TOptions options);

        protected IntegrationTestEnvironment(TOptions options = null)
        {
            options ??= new TOptions();

            EnvironmentHelper.SetRunningInTestFlag();

            this.entrypoint = GetEntrypoint(options);
            this.ServiceProvider = this.entrypoint.ScopeProvider;
        }

        protected async Task InitializeAsync()
        {
            await this.entrypoint.WaitUntilReadyAsync();
        }

        public async Task WithFreshDataContext(Func<DataContext, Task> action)
        {
            await WithFreshDataContext<object>(async (dataContext) =>
            {
                await action(dataContext);
                return null;
            });
        }

        public async Task<T> WithFreshDataContext<T>(Func<DataContext, Task<T>> action)
        {
            using var freshScope = this.entrypoint.RootProvider.CreateScope();
            await using var dataContext = freshScope.ServiceProvider.GetRequiredService<DataContext>();

            var result = await action(dataContext);
            await dataContext.SaveChangesAsync();
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            await DowngradeDatabaseAsync();
            await this.entrypoint.DisposeAsync();
        }

        private async Task DowngradeDatabaseAsync()
        {
            try
            {
                await WithFreshDataContext(async dataContext => await dataContext
                    .GetService<IMigrator>()
                    .MigrateAsync(Migration.InitialDatabase));
            }
            catch (SqlException)
            {
                //ignored.
            }
        }
    }
}
