using System;
using EyeClinicApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328190000_AppointmentWorkflowRevamp")]
    public partial class AppointmentWorkflowRevamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_UserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Appointments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AppointmentDate",
                table: "Appointments",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO TimeSlots (StartTime, EndTime, IsActive, Label)
                VALUES ('09:00:00', '09:30:00', 1, '09:00 AM - 09:30 AM');
            ");

            migrationBuilder.AddColumn<int>(
                name: "TimeSlotId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Appointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Appointments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByAdminId",
                table: "Appointments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Appointments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "Walk-in Client");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhoneNumber",
                table: "Appointments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "0000000000");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Appointments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "0000000000");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Appointments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForVisit",
                table: "Appointments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Appointments
                SET
                    Status = CASE WHEN Status = 'Confirmed' THEN 'Approved' ELSE Status END,
                    Name = ISNULL(NULLIF(Name, ''), 'Existing Client'),
                    PhoneNumber = ISNULL(NULLIF(PhoneNumber, ''), '0000000000'),
                    NormalizedPhoneNumber = ISNULL(NULLIF(NormalizedPhoneNumber, ''), '0000000000');
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate_TimeSlotId",
                table: "Appointments",
                columns: new[] { "AppointmentDate", "TimeSlotId" },
                unique: true,
                filter: "[Status] <> 'Rejected' AND [Status] <> 'Completed'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ModifiedByAdminId",
                table: "Appointments",
                column: "ModifiedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_NormalizedPhoneNumber_Status",
                table: "Appointments",
                columns: new[] { "NormalizedPhoneNumber", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_StartTime_EndTime",
                table: "TimeSlots",
                columns: new[] { "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_ModifiedByAdminId",
                table: "Appointments",
                column: "ModifiedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TimeSlots_TimeSlotId",
                table: "Appointments",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Appointments_AspNetUsers_ModifiedByAdminId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_Appointments_TimeSlots_TimeSlotId", table: "Appointments");

            migrationBuilder.DropTable(name: "TimeSlots");

            migrationBuilder.DropIndex(name: "IX_Appointments_AppointmentDate_TimeSlotId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_ModifiedByAdminId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_NormalizedPhoneNumber_Status", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_TimeSlotId", table: "Appointments");

            migrationBuilder.DropColumn(name: "Age", table: "Appointments");
            migrationBuilder.DropColumn(name: "Address", table: "Appointments");
            migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "Appointments");
            migrationBuilder.DropColumn(name: "Email", table: "Appointments");
            migrationBuilder.DropColumn(name: "ModifiedByAdminId", table: "Appointments");
            migrationBuilder.DropColumn(name: "Name", table: "Appointments");
            migrationBuilder.DropColumn(name: "NormalizedPhoneNumber", table: "Appointments");
            migrationBuilder.DropColumn(name: "PhoneNumber", table: "Appointments");
            migrationBuilder.DropColumn(name: "ReasonForVisit", table: "Appointments");
            migrationBuilder.DropColumn(name: "RowVersion", table: "Appointments");
            migrationBuilder.DropColumn(name: "TimeSlotId", table: "Appointments");
            migrationBuilder.DropColumn(name: "UpdatedAtUtc", table: "Appointments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AppointmentDate",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Appointments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_UserId",
                table: "Appointments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
