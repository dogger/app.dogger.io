using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class FixClusterRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Users_UserId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_UserId",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Instances");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Instances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_UserId",
                table: "Instances",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Users_UserId",
                table: "Instances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
