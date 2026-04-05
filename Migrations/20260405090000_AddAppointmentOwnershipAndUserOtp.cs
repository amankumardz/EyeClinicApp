using System;
using EyeClinicApp.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EyeClinicApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260405090000_AddAppointmentOwnershipAndUserOtp")]
    public partial class AddAppointmentOwnershipAndUserOtp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Appointments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOtps_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId_AppointmentDate",
                table: "Appointments",
                columns: new[] { "UserId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOtps_UserId_Purpose_ExpiryTime",
                table: "UserOtps",
                columns: new[] { "UserId", "Purpose", "ExpiryTime" });

            migrationBuilder.Sql(
                """
                UPDATE a
                SET a.UserId = u.Id
                FROM Appointments a
                INNER JOIN AspNetUsers u ON LOWER(LTRIM(RTRIM(ISNULL(a.Email, '')))) = LOWER(LTRIM(RTRIM(ISNULL(u.Email, ''))))
                WHERE a.UserId IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_UserId",
                table: "Appointments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_UserId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "UserOtps");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId_AppointmentDate",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Appointments");
        }
    }
}
