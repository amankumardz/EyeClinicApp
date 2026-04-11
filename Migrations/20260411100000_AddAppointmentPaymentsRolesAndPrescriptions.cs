using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    public partial class AddAppointmentPaymentsRolesAndPrescriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDoctorId",
                table: "Appointments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Appointments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Appointments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayOrderId",
                table: "Appointments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpayPaymentId",
                table: "Appointments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(name: "LeftEyeAxis", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "LeftEyeCyl", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "LeftEyeSph", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RightEyeAxis", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RightEyeCyl", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RightEyeSph", table: "CartItems", type: "nvarchar(120)", maxLength: 120, nullable: true);

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RightEyeSph = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RightEyeCyl = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RightEyeAxis = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeftEyeSph = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeftEyeCyl = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeftEyeAxis = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey("FK_Prescriptions_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Prescriptions_AspNetUsers_DoctorId", x => x.DoctorId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_Appointments_AssignedDoctorId", table: "Appointments", column: "AssignedDoctorId");
            migrationBuilder.CreateIndex(name: "IX_Prescriptions_AppointmentId", table: "Prescriptions", column: "AppointmentId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Prescriptions_DoctorId", table: "Prescriptions", column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_AssignedDoctorId",
                table: "Appointments",
                column: "AssignedDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Appointments_AspNetUsers_AssignedDoctorId", table: "Appointments");
            migrationBuilder.DropTable(name: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_Appointments_AssignedDoctorId", table: "Appointments");

            migrationBuilder.DropColumn(name: "AssignedDoctorId", table: "Appointments");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Appointments");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "Appointments");
            migrationBuilder.DropColumn(name: "RazorpayOrderId", table: "Appointments");
            migrationBuilder.DropColumn(name: "RazorpayPaymentId", table: "Appointments");

            migrationBuilder.DropColumn(name: "LeftEyeAxis", table: "CartItems");
            migrationBuilder.DropColumn(name: "LeftEyeCyl", table: "CartItems");
            migrationBuilder.DropColumn(name: "LeftEyeSph", table: "CartItems");
            migrationBuilder.DropColumn(name: "RightEyeAxis", table: "CartItems");
            migrationBuilder.DropColumn(name: "RightEyeCyl", table: "CartItems");
            migrationBuilder.DropColumn(name: "RightEyeSph", table: "CartItems");
        }
    }
}
