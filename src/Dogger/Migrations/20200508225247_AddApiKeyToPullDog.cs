using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddApiKeyToPullDog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM PullDogPullRequests");
            migrationBuilder.Sql("DELETE FROM PullDogRepositories");
            migrationBuilder.Sql("DELETE FROM PullDogSettings");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedApiKey",
                table: "PullDogSettings",
                nullable: false,
                defaultValue: Array.Empty<byte>());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedApiKey",
                table: "PullDogSettings");
        }
    }
}
