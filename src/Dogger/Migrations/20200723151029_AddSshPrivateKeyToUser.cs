using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Migrations
{
    public partial class AddSshPrivateKeyToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedSshPrivateKey",
                table: "Users",
                nullable: false,
                defaultValue: new byte[] {  });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedSshPrivateKey",
                table: "Users");
        }
    }
}
