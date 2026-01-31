using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDnsResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DnsResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueriedHostname = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ResolvedIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Ttl = table.Column<int>(type: "integer", nullable: false),
                    ClientIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DnsResolutions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DnsResolutions_TenantId_QueriedHostname",
                table: "DnsResolutions",
                columns: new[] { "TenantId", "QueriedHostname" });

            migrationBuilder.CreateIndex(
                name: "IX_DnsResolutions_TenantId_ResolvedAt",
                table: "DnsResolutions",
                columns: new[] { "TenantId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DnsResolutions_TenantId_ResolvedIp",
                table: "DnsResolutions",
                columns: new[] { "TenantId", "ResolvedIp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DnsResolutions");
        }
    }
}
