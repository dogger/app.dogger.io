using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class MoveGitHubInstallationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GitHubInstallationId",
                table: "PullDogRepositories",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE PullDogRepositories
SET 
    GitHubInstallationId = pds.GitHubInstallationId
FROM 
    PullDogRepositories pdr
LEFT JOIN 
    PullDogSettings pds ON pdr.PullDogSettingsId = pds.Id
");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId",
                table: "PullDogRepositories",
                column: "GitHubInstallationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PullDogRepositories_GitHubInstallationId",
                table: "PullDogRepositories");

            migrationBuilder.DropColumn(
                name: "GitHubInstallationId",
                table: "PullDogRepositories");
        }
    }
}
