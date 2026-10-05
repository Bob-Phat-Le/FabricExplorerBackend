using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class ThirdMigration_AddFabricOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Operations");
        }
    }
}
