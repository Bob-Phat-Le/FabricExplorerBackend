using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class ThirdMigration_RemoveUserPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.RenameColumn(
                name: "UserPreferenceId",
                table: "Users",
                newName: "ActiveConnectionId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "ActiveConnectionId",
                value: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveConnectionId",
                table: "Users",
                column: "ActiveConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Connections_ActiveConnectionId",
                table: "Users",
                column: "ActiveConnectionId",
                principalTable: "Connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Connections_ActiveConnectionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ActiveConnectionId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ActiveConnectionId",
                table: "Users",
                newName: "UserPreferenceId");

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUsedConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Connections_LastUsedConnectionId",
                        column: x => x.LastUsedConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "UserPreferences",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "IsDeleted", "LastUsedConnectionId", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, new Guid("33333333-3333-3333-3333-333333333333"), null, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UserPreferenceId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_LastUsedConnectionId",
                table: "UserPreferences",
                column: "LastUsedConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }
    }
}
