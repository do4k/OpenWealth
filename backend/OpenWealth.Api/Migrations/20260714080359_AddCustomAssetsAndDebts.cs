using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomAssetsAndDebts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OtherAssets",
                table: "NetWorthSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherDebts",
                table: "NetWorthSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CustomAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExpectedAnnualGrowthPercent = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomAssets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomDebts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualInterestRatePercent = table.Column<decimal>(type: "TEXT", nullable: true),
                    MonthlyPayment = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDebts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDebts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomAssets_UserId",
                table: "CustomAssets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDebts_UserId",
                table: "CustomDebts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomAssets");

            migrationBuilder.DropTable(
                name: "CustomDebts");

            migrationBuilder.DropColumn(
                name: "OtherAssets",
                table: "NetWorthSnapshots");

            migrationBuilder.DropColumn(
                name: "OtherDebts",
                table: "NetWorthSnapshots");
        }
    }
}
