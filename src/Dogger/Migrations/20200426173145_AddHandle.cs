using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddHandle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clusters_UserId",
                table: "Clusters");

            migrationBuilder.AddColumn<string>(
                name: "PullDogPullRequestHandle",
                table: "Instances",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Clusters",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_UserId_Name",
                table: "Clusters",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clusters_UserId_Name",
                table: "Clusters");

            migrationBuilder.DropColumn(
                name: "PullDogPullRequestHandle",
                table: "Instances");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Clusters",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_UserId",
                table: "Clusters",
                column: "UserId");
        }
    }
}
