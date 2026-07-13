using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyBenefits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChildBenefitAdditionalChildWeekly",
                table: "TaxSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 17.25m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChildBenefitFirstChildWeekly",
                table: "TaxSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 26.05m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChildcareIncomeLimit",
                table: "TaxSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 100000m);

            migrationBuilder.AddColumn<decimal>(
                name: "HicbcLowerThreshold",
                table: "TaxSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 60000m);

            migrationBuilder.AddColumn<decimal>(
                name: "HicbcUpperThreshold",
                table: "TaxSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 80000m);

            migrationBuilder.AddColumn<int>(
                name: "ChildrenReceivingChildBenefit",
                table: "IncomeDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildBenefitAdditionalChildWeekly",
                table: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "ChildBenefitFirstChildWeekly",
                table: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "ChildcareIncomeLimit",
                table: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "HicbcLowerThreshold",
                table: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "HicbcUpperThreshold",
                table: "TaxSettings");

            migrationBuilder.DropColumn(
                name: "ChildrenReceivingChildBenefit",
                table: "IncomeDetails");
        }
    }
}
