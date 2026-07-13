using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaydayAutomationAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualGrowthPercent",
                table: "Investments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutomationEnabled",
                table: "IncomeDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastAccrualDate",
                table: "IncomeDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaydayDayOfMonth",
                table: "IncomeDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AccrualEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    NewBalance = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccrualEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccrualEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetWorthSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NetWorth = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAssets = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalLiabilities = table.Column<decimal>(type: "TEXT", nullable: false),
                    Property = table.Column<decimal>(type: "TEXT", nullable: false),
                    Savings = table.Column<decimal>(type: "TEXT", nullable: false),
                    Investments = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mortgages = table.Column<decimal>(type: "TEXT", nullable: false),
                    StudentLoans = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetWorthSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetWorthSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccrualEvents_UserId_Date",
                table: "AccrualEvents",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_NetWorthSnapshots_UserId_Date",
                table: "NetWorthSnapshots",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccrualEvents");

            migrationBuilder.DropTable(
                name: "NetWorthSnapshots");

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualGrowthPercent",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "AutomationEnabled",
                table: "IncomeDetails");

            migrationBuilder.DropColumn(
                name: "LastAccrualDate",
                table: "IncomeDetails");

            migrationBuilder.DropColumn(
                name: "PaydayDayOfMonth",
                table: "IncomeDetails");
        }
    }
}
