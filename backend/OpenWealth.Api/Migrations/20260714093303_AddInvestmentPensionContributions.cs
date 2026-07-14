using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentPensionContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceivesIncomePensionContributions",
                table: "Investments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivesIncomePensionContributions",
                table: "Investments");
        }
    }
}
