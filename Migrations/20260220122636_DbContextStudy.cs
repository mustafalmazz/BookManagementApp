using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class DbContextStudy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySession_Users_UserId",
                table: "StudySession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudySession",
                table: "StudySession");

            migrationBuilder.RenameTable(
                name: "StudySession",
                newName: "StudySessions");

            migrationBuilder.RenameIndex(
                name: "IX_StudySession_UserId",
                table: "StudySessions",
                newName: "IX_StudySessions_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudySessions",
                table: "StudySessions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessions_Users_UserId",
                table: "StudySessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessions_Users_UserId",
                table: "StudySessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudySessions",
                table: "StudySessions");

            migrationBuilder.RenameTable(
                name: "StudySessions",
                newName: "StudySession");

            migrationBuilder.RenameIndex(
                name: "IX_StudySessions_UserId",
                table: "StudySession",
                newName: "IX_StudySession_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudySession",
                table: "StudySession",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySession_Users_UserId",
                table: "StudySession",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
