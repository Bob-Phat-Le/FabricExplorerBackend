using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class SecondMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Connections",
                columns: new[] { "Id", "ClientId", "ClientSecret", "CreatedAt", "CreatedBy", "DeletedAt", "IsDeleted", "Name", "TenantId", "UpdatedAt", "UpdatedBy", "WorkspaceId" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b"), "8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b", new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, "Production Fabric", new Guid("72f988bf-86f1-41af-91ab-2d7cd011db47"), null, null, new Guid("12345678-abcd-1234-abcd-1234567890ab") },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("3a2b1c4d-8e7f-6a5b-4c3d-2e1f0a9b8c7d"), "secret", new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, "Staging Analytics", new Guid("72f988bf-86f1-41af-91ab-2d7cd011db47"), null, null, new Guid("87654321-abcd-4321-abcd-0987654321ba") }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "IsDeleted", "UpdatedAt", "UpdatedBy", "UserName", "UserPreferenceId" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, "john.doe@company.com", false, null, null, "John Doe", new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.InsertData(
                table: "UserPreferences",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "IsDeleted", "LastUsedConnectionId", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, new Guid("33333333-3333-3333-3333-333333333333"), null, null, new Guid("11111111-1111-1111-1111-111111111111") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Connections",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "UserPreferences",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Connections",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
