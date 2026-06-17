using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SacoStayAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedSpaceAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "RoomPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SharedSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizedRoomId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedSpaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceShortlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceShortlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceShortlists_RoomPosts_RoomId",
                        column: x => x.RoomId,
                        principalTable: "RoomPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaceShortlists_SharedSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SharedSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortlistId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteStatus = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomVotes_SpaceShortlists_ShortlistId",
                        column: x => x.ShortlistId,
                        principalTable: "SpaceShortlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomVotes_ShortlistId",
                table: "RoomVotes",
                column: "ShortlistId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceShortlists_RoomId",
                table: "SpaceShortlists",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceShortlists_SpaceId",
                table: "SpaceShortlists",
                column: "SpaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomVotes");

            migrationBuilder.DropTable(
                name: "SpaceShortlists");

            migrationBuilder.DropTable(
                name: "SharedSpaces");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RoomPosts");
        }
    }
}
