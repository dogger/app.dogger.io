using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class RemoveOldInstallationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogSettings_GitHubInstallationId",
                table: "PullDogSettings");

            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId",
                table: "PullDogRepositories");

            migrationBuilder.DropColumn(
                name: "GitHubInstallationId",
                table: "PullDogSettings");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId_PullDogSettingsId",
                table: "PullDogRepositories",
                columns: new[] { "GitHubInstallationId", "PullDogSettingsId" },
                unique: true,
                filter: "[GitHubInstallationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId_PullDogSettingsId",
                table: "PullDogRepositories");

            migrationBuilder.AddColumn<long>(
                name: "GitHubInstallationId",
                table: "PullDogSettings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullDogSettings_GitHubInstallationId",
                table: "PullDogSettings",
                column: "GitHubInstallationId",
                unique: true,
                filter: "[GitHubInstallationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId",
                table: "PullDogRepositories",
                column: "GitHubInstallationId");
        }
    }
}
