using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWealth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviterUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviteeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdLinks_Users_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseholdLinks_Users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdLinks_InviteeUserId",
                table: "HouseholdLinks",
                column: "InviteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdLinks_InviterUserId_InviteeUserId",
                table: "HouseholdLinks",
                columns: new[] { "InviterUserId", "InviteeUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdLinks");
        }
    }
}
