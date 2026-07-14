using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtPayoffReinvestment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReinvestDestinationId",
                table: "Mortgages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReinvestDestinationType",
                table: "Mortgages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReinvestMonthlyAmount",
                table: "Mortgages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReinvestDestinationId",
                table: "CustomDebts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReinvestDestinationType",
                table: "CustomDebts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReinvestMonthlyAmount",
                table: "CustomDebts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReinvestDestinationId",
                table: "Mortgages");

            migrationBuilder.DropColumn(
                name: "ReinvestDestinationType",
                table: "Mortgages");

            migrationBuilder.DropColumn(
                name: "ReinvestMonthlyAmount",
                table: "Mortgages");

            migrationBuilder.DropColumn(
                name: "ReinvestDestinationId",
                table: "CustomDebts");

            migrationBuilder.DropColumn(
                name: "ReinvestDestinationType",
                table: "CustomDebts");

            migrationBuilder.DropColumn(
                name: "ReinvestMonthlyAmount",
                table: "CustomDebts");
        }
    }
}
