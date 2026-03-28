using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    [Migration("20260328233000_ConvertImagesToBase64")]
    public partial class ConvertImagesToBase64 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageBase64",
                table: "Glasses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageBase64",
                table: "PersonProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientImageBase64",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Glasses");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "PersonProfiles");

            migrationBuilder.DropColumn(
                name: "ClientImageUrl",
                table: "Reviews");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Glasses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "PersonProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "https://via.placeholder.com/150");

            migrationBuilder.AddColumn<string>(
                name: "ClientImageUrl",
                table: "Reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "https://via.placeholder.com/150");

            migrationBuilder.DropColumn(
                name: "ImageBase64",
                table: "Glasses");

            migrationBuilder.DropColumn(
                name: "ProfileImageBase64",
                table: "PersonProfiles");

            migrationBuilder.DropColumn(
                name: "ClientImageBase64",
                table: "Reviews");
        }
    }
}
