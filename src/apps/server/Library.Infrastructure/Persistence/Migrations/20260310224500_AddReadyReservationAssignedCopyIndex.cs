using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LibraryDbContext))]
    [Migration("20260310224500_AddReadyReservationAssignedCopyIndex")]
    public partial class AddReadyReservationAssignedCopyIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_AssignedCopyId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AssignedCopyId",
                table: "Reservations",
                column: "AssignedCopyId",
                unique: true,
                filter: "\"AssignedCopyId\" IS NOT NULL AND \"Status\" = 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_AssignedCopyId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AssignedCopyId",
                table: "Reservations",
                column: "AssignedCopyId");
        }
    }
}
