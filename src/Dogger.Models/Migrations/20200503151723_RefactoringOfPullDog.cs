using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class RefactoringOfPullDog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_PullDogRepositories_PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerComposeYmlFilePaths",
                table: "PullDogRepositories");

            migrationBuilder.DropColumn(
                name: "PullDogPullRequestHandle",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "PullDogRepositoryId",
                table: "Instances");

            migrationBuilder.CreateTable(
                name: "PullDogPullRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(nullable: false),
                    PullDogRepositoryId = table.Column<Guid>(nullable: false),
                    InstanceId = table.Column<Guid>(nullable: true),
                    Handle = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullDogPullRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullDogPullRequests_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PullDogPullRequests_PullDogRepositories_PullDogRepositoryId",
                        column: x => x.PullDogRepositoryId,
                        principalTable: "PullDogRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PullDogPullRequests_InstanceId",
                table: "PullDogPullRequests",
                column: "InstanceId",
                unique: true,
                filter: "[InstanceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PullDogPullRequests_PullDogRepositoryId",
                table: "PullDogPullRequests",
                column: "PullDogRepositoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullDogPullRequests");

            migrationBuilder.AddColumn<string>(
                name: "DockerComposeYmlFilePaths",
                table: "PullDogRepositories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PullDogPullRequestHandle",
                table: "Instances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PullDogRepositoryId",
                table: "Instances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_PullDogRepositoryId",
                table: "Instances",
                column: "PullDogRepositoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_PullDogRepositories_PullDogRepositoryId",
                table: "Instances",
                column: "PullDogRepositoryId",
                principalTable: "PullDogRepositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
