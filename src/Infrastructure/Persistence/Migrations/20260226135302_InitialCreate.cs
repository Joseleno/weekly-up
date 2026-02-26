using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "manual_metrics",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    sales_count = table.Column<int>(type: "integer", nullable: true),
                    new_customers = table.Column<int>(type: "integer", nullable: true),
                    visits = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_preferences",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    send_day = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    send_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    enabled_sections = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    week_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    demographics = table.Column<string>(type: "jsonb", nullable: true),
                    email_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    whatsapp_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    insights = table.Column<string>(type: "jsonb", nullable: true),
                    metrics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    business_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    business_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Free"),
                    external_auth_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integrations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    provider_account_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    property_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_integrations_users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_integrations_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integrations_user_provider",
                schema: "public",
                table: "integrations",
                columns: new[] { "user_id", "provider" },
                unique: true,
                filter: "status != 'Disconnected'");

            migrationBuilder.CreateIndex(
                name: "IX_integrations_UserId1",
                schema: "public",
                table: "integrations",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "ix_manual_metrics_user_week",
                schema: "public",
                table: "manual_metrics",
                columns: new[] { "user_id", "week_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_preferences_user",
                schema: "public",
                table: "report_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_user_created",
                schema: "public",
                table: "reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_external_auth_id",
                schema: "public",
                table: "users",
                column: "external_auth_id",
                unique: true,
                filter: "external_auth_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integrations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "manual_metrics",
                schema: "public");

            migrationBuilder.DropTable(
                name: "report_preferences",
                schema: "public");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");
        }
    }
}
