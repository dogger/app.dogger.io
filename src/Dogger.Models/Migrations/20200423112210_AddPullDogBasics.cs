using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddPullDogBasics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PullDogRepositoryId",
                table: "Instances",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Clusters",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PullDogSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    PlanId = table.Column<string>(nullable: false),
                    PoolSize = table.Column<int>(nullable: true),
                    GitHubInstallationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullDogSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullDogSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PullDogRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PullDogSettingsId = table.Column<Guid>(nullable: true),
                    Handle = table.Column<string>(nullable: false),
                    DockerComposeYmlFilePaths = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullDogRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullDogRepositories_PullDogSettings_PullDogSettingsId",
                        column: x => x.PullDogSettingsId,
                        principalTable: "PullDogSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Instances_PullDogRepositoryId",
                table: "Instances",
                column: "PullDogRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_PullDogSettingsId",
                table: "PullDogRepositories",
                column: "PullDogSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogSettings_UserId",
                table: "PullDogSettings",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_PullDogRepositories_PullDogRepositoryId",
                table: "Instances",
                column: "PullDogRepositoryId",
                principalTable: "PullDogRepositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_PullDogRepositories_PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.DropTable(
                name: "PullDogRepositories");

            migrationBuilder.DropTable(
                name: "PullDogSettings");

            migrationBuilder.DropIndex(
                name: "IX_Instances_PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Clusters");
        }
    }
}
