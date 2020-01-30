using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddClusters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClusterId",
                table: "Instances",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clusters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Instances_ClusterId",
                table: "Instances",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_UserId",
                table: "Clusters",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances",
                column: "ClusterId",
                principalTable: "Clusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances");

            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropIndex(
                name: "IX_Instances_ClusterId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "Instances");
        }
    }
}
