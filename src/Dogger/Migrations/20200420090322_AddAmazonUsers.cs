using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddAmazonUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmazonUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    EncryptedAccessKeyId = table.Column<byte[]>(nullable: false),
                    EncryptedSecretAccessKey = table.Column<byte[]>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmazonUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmazonUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmazonUsers_Name",
                table: "AmazonUsers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmazonUsers_UserId",
                table: "AmazonUsers",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmazonUsers");
        }
    }
}
