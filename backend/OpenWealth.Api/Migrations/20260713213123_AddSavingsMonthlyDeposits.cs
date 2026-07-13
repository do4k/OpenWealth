using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsMonthlyDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDeposit",
                table: "SavingsAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "AccrualEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyDeposit",
                table: "SavingsAccounts");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "AccrualEvents");
        }
    }
}
