using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuPairProfileYearsOfExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "AuPairProfiles");

            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "AuPairProfiles",
                newName: "Suburb");

            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "AuPairProfiles",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "HasCar",
                table: "AuPairProfiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFirstAid",
                table: "AuPairProfiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "AuPairProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Availability",
                table: "AuPairProfiles");

            migrationBuilder.DropColumn(
                name: "HasCar",
                table: "AuPairProfiles");

            migrationBuilder.DropColumn(
                name: "HasFirstAid",
                table: "AuPairProfiles");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "AuPairProfiles");

            migrationBuilder.RenameColumn(
                name: "Suburb",
                table: "AuPairProfiles",
                newName: "Experience");

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "AuPairProfiles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
