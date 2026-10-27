using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricExplorerBackend.Infrastructures.Persistences.Migrations
{
    /// <inheritdoc />
    public partial class Update_Notification_IsRead_ToComputedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Notifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
