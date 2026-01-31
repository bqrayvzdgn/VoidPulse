using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentCaptureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessName",
                table: "TrafficFlows",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedHostname",
                table: "TrafficFlows",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TlsSni",
                table: "TrafficFlows",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessName",
                table: "TrafficFlows");

            migrationBuilder.DropColumn(
                name: "ResolvedHostname",
                table: "TrafficFlows");

            migrationBuilder.DropColumn(
                name: "TlsSni",
                table: "TrafficFlows");
        }
    }
}
