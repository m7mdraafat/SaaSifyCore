using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSifyCore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email_TenantId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_TenantId",
                table: "Users",
                columns: new[] { "Email", "TenantId" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "PasswordHash", "FirstName", "LastName", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_Status_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "Token", "IsRevoked", "ExpiresAt" },
                filter: "\"IsRevoked\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Status_CreatedAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token_Status_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_Status_CreatedAt",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_TenantId",
                table: "Users",
                columns: new[] { "Email", "TenantId" });
        }
    }
}
