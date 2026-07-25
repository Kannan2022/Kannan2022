using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationAuditService.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PaidByUserId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SplitType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SharedExpenseId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ShareAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseParticipants_SharedExpenses_SharedExpenseId",
                        column: x => x.SharedExpenseId,
                        principalTable: "SharedExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseParticipants_SharedExpenseId",
                table: "ExpenseParticipants",
                column: "SharedExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_OrganizationId",
                table: "SharedExpenses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_OrganizationId_GroupId",
                table: "SharedExpenses",
                columns: new[] { "OrganizationId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_OrganizationId_GroupId_CreatedAt",
                table: "SharedExpenses",
                columns: new[] { "OrganizationId", "GroupId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseParticipants");

            migrationBuilder.DropTable(
                name: "SharedExpenses");
        }
    }
}
