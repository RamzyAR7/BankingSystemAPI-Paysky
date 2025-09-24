using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSystemAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NationalId_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserName_BankId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "BankId",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email_BankId",
                table: "AspNetUsers",
                columns: new[] { "Email", "BankId" },
                unique: true,
                filter: "[BankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NationalId_BankId",
                table: "AspNetUsers",
                columns: new[] { "NationalId", "BankId" },
                unique: true,
                filter: "[BankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber_BankId",
                table: "AspNetUsers",
                columns: new[] { "PhoneNumber", "BankId" },
                unique: true,
                filter: "[BankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName_BankId",
                table: "AspNetUsers",
                columns: new[] { "UserName", "BankId" },
                unique: true,
                filter: "[BankId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NationalId_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber_BankId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserName_BankId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "BankId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email_BankId",
                table: "AspNetUsers",
                columns: new[] { "Email", "BankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NationalId_BankId",
                table: "AspNetUsers",
                columns: new[] { "NationalId", "BankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber_BankId",
                table: "AspNetUsers",
                columns: new[] { "PhoneNumber", "BankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName_BankId",
                table: "AspNetUsers",
                columns: new[] { "UserName", "BankId" },
                unique: true);
        }
    }
}
