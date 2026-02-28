using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationTokenAndFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_token",
                schema: "public",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                schema: "public",
                table: "reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                schema: "public",
                table: "reports");
        }
    }
}
