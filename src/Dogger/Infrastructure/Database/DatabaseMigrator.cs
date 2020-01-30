using System.Diagnostics;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dogger.Infrastructure.Database
{
    public class DatabaseMigrator : IDatabaseMigrator
    {
        private readonly DataContext dataContext;

        public DatabaseMigrator(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task MigrateAsync()
        {
            if (dataContext.Database.IsInMemory())
                return;

            await dataContext.Database.MigrateAsync();
        }

        public static async Task MigrateDatabaseForHostAsync(IHost host)
        {
            if (Debugger.IsAttached)
                return;

            using var scope = host.Services.CreateScope();

            var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
            await migrator.MigrateAsync();
        }
    }
}
