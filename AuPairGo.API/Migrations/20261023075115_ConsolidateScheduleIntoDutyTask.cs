using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateScheduleIntoDutyTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.AddColumn<int>(
                name: "DropOffAddressId",
                table: "DutyTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PickupAddressId",
                table: "DutyTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DutyTasks_DropOffAddressId",
                table: "DutyTasks",
                column: "DropOffAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyTasks_PickupAddressId",
                table: "DutyTasks",
                column: "PickupAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_DutyTasks_Addresses_DropOffAddressId",
                table: "DutyTasks",
                column: "DropOffAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DutyTasks_Addresses_PickupAddressId",
                table: "DutyTasks",
                column: "PickupAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DutyTasks_Addresses_DropOffAddressId",
                table: "DutyTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_DutyTasks_Addresses_PickupAddressId",
                table: "DutyTasks");

            migrationBuilder.DropIndex(
                name: "IX_DutyTasks_DropOffAddressId",
                table: "DutyTasks");

            migrationBuilder.DropIndex(
                name: "IX_DutyTasks_PickupAddressId",
                table: "DutyTasks");

            migrationBuilder.DropColumn(
                name: "DropOffAddressId",
                table: "DutyTasks");

            migrationBuilder.DropColumn(
                name: "PickupAddressId",
                table: "DutyTasks");

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    DropOffAddressId = table.Column<int>(type: "int", nullable: false),
                    ExpectedDropOffTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    PickupAddressId = table.Column<int>(type: "int", nullable: false),
                    PickupTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
