using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APPDEV_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===== STEP 1: Delete existing clients to avoid unique constraint violation =====
            // The old clients don't have users and can't be linked
            // We'll remove them so new users can create fresh profiles
            migrationBuilder.Sql("DELETE FROM Clients");

            // ===== STEP 2: Add UserId column as nullable first =====
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: true);  // ===== CHANGED: nullable: true instead of false =====

            // ===== STEP 3: Create the Users table =====
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            // ===== STEP 4: Create unique index on UserId =====
            // Now this won't fail because UserId is nullable and we deleted duplicate nulls
            migrationBuilder.CreateIndex(
                name: "IX_Clients_UserId",
                table: "Clients",
                column: "UserId",
                unique: true);

            // ===== STEP 5: Create foreign key relationship =====
            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Users_UserId",
                table: "Clients",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Users_UserId",
                table: "Clients");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Clients_UserId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Clients");
        }
    }
}
