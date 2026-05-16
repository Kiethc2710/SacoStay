using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SacoStayAPI.Migrations
{
    /// <inheritdoc />
    public partial class DB_UserLifeStyle_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserLifestyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LifestyleOptionId = table.Column<int>(type: "integer", nullable: false),
                    LifestyleQuestionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLifestyles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLifestyles_LifestyleOptions_LifestyleOptionId",
                        column: x => x.LifestyleOptionId,
                        principalTable: "LifestyleOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLifestyles_LifestyleQuestions_LifestyleQuestionId",
                        column: x => x.LifestyleQuestionId,
                        principalTable: "LifestyleQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLifestyles_LifestyleOptionId",
                table: "UserLifestyles",
                column: "LifestyleOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLifestyles_LifestyleQuestionId",
                table: "UserLifestyles",
                column: "LifestyleQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLifestyles");
        }
    }
}
