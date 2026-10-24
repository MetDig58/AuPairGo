using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuPairGo.API.Migrations
{
    /// <inheritdoc />
    public partial class AddParentOwnershipToAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentProfileId",
                table: "Addresses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ParentProfileId",
                table: "Addresses",
                column: "ParentProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_ParentProfiles_ParentProfileId",
                table: "Addresses",
                column: "ParentProfileId",
                principalTable: "ParentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_ParentProfiles_ParentProfileId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_ParentProfileId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "ParentProfileId",
                table: "Addresses");
        }
    }
}
