using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                table: "PullDogRepositories",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogSettings_GitHubInstallationId",
                table: "PullDogSettings",
                column: "GitHubInstallationId",
                unique: true,
                filter: "[GitHubInstallationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_Handle",
                table: "PullDogRepositories",
                column: "Handle",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogSettings_GitHubInstallationId",
                table: "PullDogSettings");

            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_Handle",
                table: "PullDogRepositories");

            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                table: "PullDogRepositories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
