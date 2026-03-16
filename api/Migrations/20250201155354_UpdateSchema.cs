using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class UpdateSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nv_xu__ly_3",
                table: "nghi_phep",
                newName: "nv_xu_ly_3");

            migrationBuilder.RenameColumn(
                name: "nv_xu__ly_2",
                table: "nghi_phep",
                newName: "nv_xu_ly_2");

            migrationBuilder.AlterColumn<string>(
                name: "trang_thai",
                table: "nghi_phep",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nv_xu_ly_3",
                table: "nghi_phep",
                newName: "nv_xu__ly_3");

            migrationBuilder.RenameColumn(
                name: "nv_xu_ly_2",
                table: "nghi_phep",
                newName: "nv_xu__ly_2");

            migrationBuilder.UpdateData(
                table: "nghi_phep",
                keyColumn: "trang_thai",
                keyValue: null,
                column: "trang_thai",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "trang_thai",
                table: "nghi_phep",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
