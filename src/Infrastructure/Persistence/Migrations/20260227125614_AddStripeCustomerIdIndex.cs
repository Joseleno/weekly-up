using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeCustomerIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_users_stripe_customer_id",
                schema: "public",
                table: "users",
                column: "stripe_customer_id",
                unique: true,
                filter: "stripe_customer_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_stripe_customer_id",
                schema: "public",
                table: "users");
        }
    }
}
