﻿// <auto-generated />
using System;
using Dogger.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dogger.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20200723151029_AddSshPrivateKeyToUser")]
    partial class AddSshPrivateKeyToUser
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Dogger.Domain.Models.AmazonUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<byte[]>("EncryptedAccessKeyId")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<byte[]>("EncryptedSecretAccessKey")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("AmazonUsers");
                });

            modelBuilder.Entity("Dogger.Domain.Models.Cluster", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "Name")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL AND [Name] IS NOT NULL");

                    b.ToTable("Clusters");
                });

            modelBuilder.Entity("Dogger.Domain.Models.Identity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Identities");
                });

            modelBuilder.Entity("Dogger.Domain.Models.Instance", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ClusterId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ExpiresAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsProvisioned")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("PlanId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ClusterId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Instances");
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogPullRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ConfigurationOverride")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Handle")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid?>("InstanceId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("PullDogRepositoryId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("InstanceId")
                        .IsUnique()
                        .HasFilter("[InstanceId] IS NOT NULL");

                    b.HasIndex("PullDogRepositoryId", "Handle")
                        .IsUnique();

                    b.ToTable("PullDogPullRequests");
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogRepository", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<long?>("GitHubInstallationId")
                        .HasColumnType("bigint");

                    b.Property<string>("Handle")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("PullDogSettingsId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("PullDogSettingsId", "Handle")
                        .IsUnique();

                    b.ToTable("PullDogRepositories");
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<byte[]>("EncryptedApiKey")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("PlanId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PoolSize")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("PullDogSettings");
                });

            modelBuilder.Entity("Dogger.Domain.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<byte[]>("EncryptedSshPrivateKey")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("StripeCustomerId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StripeSubscriptionId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Dogger.Domain.Models.AmazonUser", b =>
                {
                    b.HasOne("Dogger.Domain.Models.User", "User")
                        .WithMany("AmazonUsers")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Dogger.Domain.Models.Cluster", b =>
                {
                    b.HasOne("Dogger.Domain.Models.User", "User")
                        .WithMany("Clusters")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Dogger.Domain.Models.Identity", b =>
                {
                    b.HasOne("Dogger.Domain.Models.User", "User")
                        .WithMany("Identities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dogger.Domain.Models.Instance", b =>
                {
                    b.HasOne("Dogger.Domain.Models.Cluster", "Cluster")
                        .WithMany("Instances")
                        .HasForeignKey("ClusterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogPullRequest", b =>
                {
                    b.HasOne("Dogger.Domain.Models.Instance", "Instance")
                        .WithOne("PullDogPullRequest")
                        .HasForeignKey("Dogger.Domain.Models.PullDogPullRequest", "InstanceId");

                    b.HasOne("Dogger.Domain.Models.PullDogRepository", "PullDogRepository")
                        .WithMany("PullRequests")
                        .HasForeignKey("PullDogRepositoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogRepository", b =>
                {
                    b.HasOne("Dogger.Domain.Models.PullDogSettings", "PullDogSettings")
                        .WithMany("Repositories")
                        .HasForeignKey("PullDogSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dogger.Domain.Models.PullDogSettings", b =>
                {
                    b.HasOne("Dogger.Domain.Models.User", "User")
                        .WithOne("PullDogSettings")
                        .HasForeignKey("Dogger.Domain.Models.PullDogSettings", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
