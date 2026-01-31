using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrafficFlowIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TrafficFlows_TenantId_AgentKeyId",
                table: "TrafficFlows",
                columns: new[] { "TenantId", "AgentKeyId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficFlows_TenantId_Protocol",
                table: "TrafficFlows",
                columns: new[] { "TenantId", "Protocol" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficFlows_TenantId_StartedAt",
                table: "TrafficFlows",
                columns: new[] { "TenantId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrafficFlows_TenantId_AgentKeyId",
                table: "TrafficFlows");

            migrationBuilder.DropIndex(
                name: "IX_TrafficFlows_TenantId_Protocol",
                table: "TrafficFlows");

            migrationBuilder.DropIndex(
                name: "IX_TrafficFlows_TenantId_StartedAt",
                table: "TrafficFlows");
        }
    }
}
