using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class UpdateSchemaNP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nv_ban_giao",
                table: "nghi_phep");

            migrationBuilder.AddColumn<string>(
                name: "ban_giao",
                table: "nghi_phep",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ban_giao",
                table: "nghi_phep");

            migrationBuilder.AddColumn<ulong>(
                name: "nv_ban_giao",
                table: "nghi_phep",
                type: "bigint unsigned",
                nullable: true);
        }
    }
}
