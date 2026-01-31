using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCapturedPackets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapturedPackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrafficFlowId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    DestinationIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    SourcePort = table.Column<int>(type: "integer", nullable: false),
                    DestinationPort = table.Column<int>(type: "integer", nullable: false),
                    Protocol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PacketLength = table.Column<int>(type: "integer", nullable: false),
                    HeaderBytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProtocolStack = table.Column<string>(type: "text", nullable: false),
                    Info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapturedPackets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapturedPackets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CapturedPackets_TrafficFlows_TrafficFlowId",
                        column: x => x.TrafficFlowId,
                        principalTable: "TrafficFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapturedPackets_TenantId_CapturedAt",
                table: "CapturedPackets",
                columns: new[] { "TenantId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CapturedPackets_TenantId_SourceIp_DestinationIp",
                table: "CapturedPackets",
                columns: new[] { "TenantId", "SourceIp", "DestinationIp" });

            migrationBuilder.CreateIndex(
                name: "IX_CapturedPackets_TrafficFlowId",
                table: "CapturedPackets",
                column: "TrafficFlowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapturedPackets");
        }
    }
}
