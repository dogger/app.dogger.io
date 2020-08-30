using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure;

namespace Dogger.Domain.Models
{
    public class DataContext : DbContext
    {
        public static Guid DemoClusterId => new Guid("810fb451-0c78-4451-b3ac-b504450bc7dd");
        public static Guid PullDogDemoClusterId => new Guid("1112df9e-f6a9-40e9-950a-5e372d03c9a2");
        public static Guid DoggerClusterId => new Guid("352ba80a-3f32-43fd-ab37-f97facc4a9bb");

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> action,
            IsolationLevel? isolationLevel,
            CancellationToken cancellationToken)
        {
            if (this.Database.CurrentTransaction != null)
                return await action();

            var strategy = this.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await this.Database.BeginTransactionAsync(
                    isolationLevel ?? IsolationLevel.ReadUncommitted,
                    cancellationToken);

                try
                {
                    var result = await action();
                    await transaction.CommitAsync(cancellationToken);

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Instance>()
                .HasIndex(instance => instance.Name)
                .IsUnique();

            modelBuilder
                .Entity<Identity>()
                .HasIndex(identity => identity.Name)
                .IsUnique();

            modelBuilder
                .Entity<AmazonUser>()
                .HasIndex(user => user.Name)
                .IsUnique();

            modelBuilder
                .Entity<PullDogRepository>()
                .HasIndex(repository => new
                {
                    repository.PullDogSettingsId,
                    repository.Handle
                })
                .IsUnique();

            modelBuilder
                .Entity<PullDogPullRequest>()
                .Property(x => x.ConfigurationOverride)
                .HasConversion(
                    x => JsonSerializer.Serialize(x, JsonFactory.GetOptions()),
                    x => JsonSerializer.Deserialize<ConfigurationFileOverride>(x, JsonFactory.GetOptions()));

            modelBuilder
                .Entity<PullDogPullRequest>()
                .HasIndex(pullRequest => new
                {
                    pullRequest.PullDogRepositoryId,
                    pullRequest.Handle
                })
                .IsUnique();

            modelBuilder
                .Entity<Cluster>()
                .HasIndex(cluster => new
                {
                    cluster.UserId, 
                    cluster.Name
                })
                .IsUnique();
        }

        public DbSet<AmazonUser> AmazonUsers { get; set; } = null!;
        public DbSet<PullDogSettings> PullDogSettings { get; set; } = null!;
        public DbSet<PullDogRepository> PullDogRepositories { get; set; } = null!;
        public DbSet<PullDogPullRequest> PullDogPullRequests { get; set; } = null!;
        public DbSet<Identity> Identities { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Instance> Instances { get; set; } = null!;
        public DbSet<Cluster> Clusters { get; set; } = null!;
    }
}

