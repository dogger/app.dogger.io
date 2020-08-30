using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_Handle",
                table: "PullDogRepositories");

            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_PullDogSettingsId",
                table: "PullDogRepositories");

            migrationBuilder.DropIndex(
                name: "IX_PullDogPullRequests_PullDogRepositoryId",
                table: "PullDogPullRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                table: "PullDogPullRequests",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_PullDogSettingsId_Handle",
                table: "PullDogRepositories",
                columns: new[] { "PullDogSettingsId", "Handle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullDogPullRequests_PullDogRepositoryId_Handle",
                table: "PullDogPullRequests",
                columns: new[] { "PullDogRepositoryId", "Handle" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_PullDogSettingsId_Handle",
                table: "PullDogRepositories");

            migrationBuilder.DropIndex(
                name: "IX_PullDogPullRequests_PullDogRepositoryId_Handle",
                table: "PullDogPullRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Handle",
                table: "PullDogPullRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_Handle",
                table: "PullDogRepositories",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_PullDogSettingsId",
                table: "PullDogRepositories",
                column: "PullDogSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogPullRequests_PullDogRepositoryId",
                table: "PullDogPullRequests",
                column: "PullDogRepositoryId");
        }
    }
}
