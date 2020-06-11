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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
