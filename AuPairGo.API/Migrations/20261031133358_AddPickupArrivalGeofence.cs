using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupArrivalGeofence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PickupArrivalNotified",
                table: "DutyTasks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupArrivalNotifiedAt",
                table: "DutyTasks",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupArrivalNotified",
                table: "DutyTasks");

            migrationBuilder.DropColumn(
                name: "PickupArrivalNotifiedAt",
                table: "DutyTasks");
        }
    }
}
