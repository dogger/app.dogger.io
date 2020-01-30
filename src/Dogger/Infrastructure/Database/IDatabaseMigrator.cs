using System.Threading.Tasks;

namespace Dogger.Infrastructure.Database
{
    public interface IDatabaseMigrator
    {
        Task MigrateAsync();
    }
}