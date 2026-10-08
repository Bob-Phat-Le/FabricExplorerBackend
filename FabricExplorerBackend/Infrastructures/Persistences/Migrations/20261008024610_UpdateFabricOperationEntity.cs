using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabricExplorerBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFabricOperationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConnectionId",
                table: "Operations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "Operations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Operations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                table: "Operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Operations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Operations");
        }
    }
}
