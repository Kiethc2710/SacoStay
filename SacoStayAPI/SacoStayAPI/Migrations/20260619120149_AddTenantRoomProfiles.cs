using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SacoStayAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRoomProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantRoomProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    MaxPeople = table.Column<int>(type: "integer", nullable: true),
                    Amenities = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExtraNotes = table.Column<string>(type: "text", nullable: true),
                    Images = table.Column<List<string>>(type: "text[]", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRoomProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_TenantRoomProfiles_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRoomProfiles");
        }
    }
}
