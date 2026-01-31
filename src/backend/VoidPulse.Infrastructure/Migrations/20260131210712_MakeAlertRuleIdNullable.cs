using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeAlertRuleIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts");

            migrationBuilder.AlterColumn<Guid>(
                name: "AlertRuleId",
                table: "Alerts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId",
                principalTable: "AlertRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts");

            migrationBuilder.AlterColumn<Guid>(
                name: "AlertRuleId",
                table: "Alerts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId",
                principalTable: "AlertRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
