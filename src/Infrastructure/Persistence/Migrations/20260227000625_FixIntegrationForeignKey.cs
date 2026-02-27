using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixIntegrationForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_integrations_users_UserId1",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropForeignKey(
                name: "FK_integrations_users_user_id",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "IX_integrations_UserId1",
                schema: "public",
                table: "integrations");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "public",
                table: "integrations");

            migrationBuilder.AddForeignKey(
                name: "fk_integrations_users",
                schema: "public",
                table: "integrations",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_integrations_users",
                schema: "public",
                table: "integrations");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "public",
                table: "integrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_integrations_UserId1",
                schema: "public",
                table: "integrations",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_integrations_users_UserId1",
                schema: "public",
                table: "integrations",
                column: "UserId1",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_integrations_users_user_id",
                schema: "public",
                table: "integrations",
                column: "user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
