using EyeClinicApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260329000100_AddShiftAndSlots")]
    public partial class AddShiftAndSlots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Shift",
                table: "TimeSlots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Morning");

            migrationBuilder.Sql("""
                UPDATE [TimeSlots]
                SET [Shift] = CASE
                    WHEN [StartTime] >= '09:00:00' AND [StartTime] < '12:00:00' THEN 'Morning'
                    WHEN [StartTime] >= '12:00:00' AND [StartTime] < '16:00:00' THEN 'Afternoon'
                    ELSE 'Evening'
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shift",
                table: "TimeSlots");
        }
    }
}
