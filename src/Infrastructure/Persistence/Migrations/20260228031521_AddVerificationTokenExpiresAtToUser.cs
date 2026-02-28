using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationTokenExpiresAtToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "verification_token_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_token_expires_at",
                schema: "public",
                table: "users");
        }
    }
}
