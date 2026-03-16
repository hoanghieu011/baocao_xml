using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class UpdatePhieuNghi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nv_xu_ly",
                table: "nghi_phep",
                newName: "nv_xu_ly_1");

            migrationBuilder.RenameColumn(
                name: "ngay_xu_ly",
                table: "nghi_phep",
                newName: "ngay_xu_ly_3");

            migrationBuilder.AddColumn<string>(
                name: "ly_do_nghi_str",
                table: "nghi_phep",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_xu_ly_1",
                table: "nghi_phep",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_xu_ly_2",
                table: "nghi_phep",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "nv_xu__ly_2",
                table: "nghi_phep",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "nv_xu__ly_3",
                table: "nghi_phep",
                type: "bigint unsigned",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ly_do_nghi_str",
                table: "nghi_phep");

            migrationBuilder.DropColumn(
                name: "ngay_xu_ly_1",
                table: "nghi_phep");

            migrationBuilder.DropColumn(
                name: "ngay_xu_ly_2",
                table: "nghi_phep");

            migrationBuilder.DropColumn(
                name: "nv_xu__ly_2",
                table: "nghi_phep");

            migrationBuilder.DropColumn(
                name: "nv_xu__ly_3",
                table: "nghi_phep");

            migrationBuilder.RenameColumn(
                name: "nv_xu_ly_1",
                table: "nghi_phep",
                newName: "nv_xu_ly");

            migrationBuilder.RenameColumn(
                name: "ngay_xu_ly_3",
                table: "nghi_phep",
                newName: "ngay_xu_ly");
        }
    }
}
