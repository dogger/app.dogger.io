using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class RemoveBadIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId_PullDogSettingsId",
                table: "PullDogRepositories");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId_PullDogSettingsId",
                table: "PullDogRepositories",
                columns: new[] { "GitHubInstallationId", "PullDogSettingsId" },
                unique: true,
                filter: "[GitHubInstallationId] IS NOT NULL");
        }
    }
}
