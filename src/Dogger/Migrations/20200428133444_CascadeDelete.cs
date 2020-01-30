using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class CascadeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances");

            migrationBuilder.DropForeignKey(
                name: "FK_PullDogRepositories_PullDogSettings_PullDogSettingsId",
                table: "PullDogRepositories");

            migrationBuilder.DropForeignKey(
                name: "FK_PullDogSettings_Users_UserId",
                table: "PullDogSettings");

            migrationBuilder.DropIndex(
                name: "IX_PullDogSettings_UserId",
                table: "PullDogSettings");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PullDogSettings",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PullDogSettingsId",
                table: "PullDogRepositories",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClusterId",
                table: "Instances",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullDogSettings_UserId",
                table: "PullDogSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances",
                column: "ClusterId",
                principalTable: "Clusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PullDogRepositories_PullDogSettings_PullDogSettingsId",
                table: "PullDogRepositories",
                column: "PullDogSettingsId",
                principalTable: "PullDogSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PullDogSettings_Users_UserId",
                table: "PullDogSettings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances");

            migrationBuilder.DropForeignKey(
                name: "FK_PullDogRepositories_PullDogSettings_PullDogSettingsId",
                table: "PullDogRepositories");

            migrationBuilder.DropForeignKey(
                name: "FK_PullDogSettings_Users_UserId",
                table: "PullDogSettings");

            migrationBuilder.DropIndex(
                name: "IX_PullDogSettings_UserId",
                table: "PullDogSettings");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PullDogSettings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "PullDogSettingsId",
                table: "PullDogRepositories",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "ClusterId",
                table: "Instances",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.CreateIndex(
                name: "IX_PullDogSettings_UserId",
                table: "PullDogSettings",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Clusters_ClusterId",
                table: "Instances",
                column: "ClusterId",
                principalTable: "Clusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PullDogRepositories_PullDogSettings_PullDogSettingsId",
                table: "PullDogRepositories",
                column: "PullDogSettingsId",
                principalTable: "PullDogSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PullDogSettings_Users_UserId",
                table: "PullDogSettings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
