using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLocationIdsWithDutyTaskId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "au_pair_id",
                table: "location_logs");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "location_logs");

            migrationBuilder.RenameColumn(
                name: "schedule_id",
                table: "location_logs",
                newName: "duty_task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "duty_task_id",
                table: "location_logs",
                newName: "schedule_id");

            migrationBuilder.AddColumn<int>(
                name: "au_pair_id",
                table: "location_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "location_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
