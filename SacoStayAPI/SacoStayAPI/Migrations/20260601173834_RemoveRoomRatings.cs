using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SacoStayAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoomRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomRatings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    GuestPhone = table.Column<string>(type: "text", nullable: true),
                    ImageUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    Star = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomRatings_Accounts_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomRatings_RoomPosts_RoomPostId",
                        column: x => x.RoomPostId,
                        principalTable: "RoomPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomRatings_RoomPostId_TenantId",
                table: "RoomRatings",
                columns: new[] { "RoomPostId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomRatings_TenantId",
                table: "RoomRatings",
                column: "TenantId");
        }
    }
}
