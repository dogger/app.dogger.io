using System;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dogger.Domain.Services.PullDog;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
namespace Dogger.Domain.Models
{
    public class DataContext : DbContext
    {
        public static Guid DemoClusterId => new Guid("810fb451-0c78-4451-b3ac-b504450bc7dd");
        public static Guid PullDogDemoClusterId => new Guid("1112df9e-f6a9-40e9-950a-5e372d03c9a2");
        public static Guid DoggerClusterId => new Guid("352ba80a-3f32-43fd-ab37-f97facc4a9bb");

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
                .Entity<PullDogSettings>()
                .HasIndex(settings => settings.GitHubInstallationId)
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
                    x => JsonSerializer.Serialize(x, GetJsonSerializerOptions()),
                    x => JsonSerializer.Deserialize<ConfigurationFileOverride>(x, GetJsonSerializerOptions()));

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

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        public DbSet<AmazonUser> AmazonUsers { get; set; }
        public DbSet<PullDogSettings> PullDogSettings { get; set; }
        public DbSet<PullDogRepository> PullDogRepositories { get; set; }
        public DbSet<PullDogPullRequest> PullDogPullRequests { get; set; }
        public DbSet<Identity> Identities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Instance> Instances { get; set; }
        public DbSet<Cluster> Clusters { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
