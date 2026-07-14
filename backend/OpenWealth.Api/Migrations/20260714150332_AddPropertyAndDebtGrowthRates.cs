using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyAndDebtGrowthRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualGrowthPercent",
                table: "Properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualGrowthPercent",
                table: "CustomDebts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedAnnualGrowthPercent",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualGrowthPercent",
                table: "CustomDebts");
        }
    }
}
