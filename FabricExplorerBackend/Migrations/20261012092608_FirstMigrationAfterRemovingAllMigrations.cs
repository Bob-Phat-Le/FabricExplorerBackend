using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigrationAfterRemovingAllMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientSecret = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FabricOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FabricOperationUrl = table.Column<string>(type: "text", nullable: true),
                    OperationType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NextPollAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PercentComplete = table.Column<int>(type: "integer", nullable: true),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    ActiveConnectionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Connections_ActiveConnectionId",
                        column: x => x.ActiveConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Connections",
                columns: new[] { "Id", "ClientId", "ClientSecret", "CreatedAt", "CreatedBy", "DeletedAt", "Name", "TenantId", "UpdatedAt", "UpdatedBy", "WorkspaceId" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b"), "8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b", new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, "Production Fabric", new Guid("72f988bf-86f1-41af-91ab-2d7cd011db47"), null, null, new Guid("12345678-abcd-1234-abcd-1234567890ab") },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("3a2b1c4d-8e7f-6a5b-4c3d-2e1f0a9b8c7d"), "secret", new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, "Staging Analytics", new Guid("72f988bf-86f1-41af-91ab-2d7cd011db47"), null, null, new Guid("87654321-abcd-4321-abcd-0987654321ba") }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ActiveConnectionId", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "UpdatedAt", "UpdatedBy", "UserName" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, "john.doe@company.com", null, null, "John Doe" });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_WorkspaceId",
                table: "Connections",
                column: "WorkspaceId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveConnectionId",
                table: "Users",
                column: "ActiveConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Connections");
        }
    }
}
