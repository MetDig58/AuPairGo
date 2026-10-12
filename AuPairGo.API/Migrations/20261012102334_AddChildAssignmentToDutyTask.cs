using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class AddChildAssignmentToDutyTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChildProfileId",
                table: "DutyTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DutyTasks_ChildProfileId",
                table: "DutyTasks",
                column: "ChildProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_DutyTasks_ChildProfiles_ChildProfileId",
                table: "DutyTasks",
                column: "ChildProfileId",
                principalTable: "ChildProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DutyTasks_ChildProfiles_ChildProfileId",
                table: "DutyTasks");

            migrationBuilder.DropIndex(
                name: "IX_DutyTasks_ChildProfileId",
                table: "DutyTasks");

            migrationBuilder.DropColumn(
                name: "ChildProfileId",
                table: "DutyTasks");
        }
    }
}
