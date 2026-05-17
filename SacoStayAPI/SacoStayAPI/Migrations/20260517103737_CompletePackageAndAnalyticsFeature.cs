using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SacoStayAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompletePackageAndAnalyticsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PackageExpiresAt",
                table: "RoomPosts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageTier",
                table: "RoomPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PackageName",
                table: "PaymentTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomPostId",
                table: "PaymentTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomViewHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomViewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomViewHistories_Accounts_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomViewHistories_RoomPosts_RoomPostId",
                        column: x => x.RoomPostId,
                        principalTable: "RoomPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomViewHistories_RoomPostId",
                table: "RoomViewHistories",
                column: "RoomPostId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomViewHistories_TenantId",
                table: "RoomViewHistories",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomViewHistories");

            migrationBuilder.DropColumn(
                name: "PackageExpiresAt",
                table: "RoomPosts");

            migrationBuilder.DropColumn(
                name: "PackageTier",
                table: "RoomPosts");

            migrationBuilder.DropColumn(
                name: "PackageName",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "RoomPostId",
                table: "PaymentTransactions");
        }
    }
}
