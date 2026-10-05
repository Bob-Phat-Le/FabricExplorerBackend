using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceId_Unique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Connections_WorkspaceId",
                table: "Connections",
                column: "WorkspaceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Connections_WorkspaceId",
                table: "Connections");
        }
    }
}
