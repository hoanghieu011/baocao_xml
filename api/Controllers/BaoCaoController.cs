using API.Common;
using API.Data;
using API.DTO;
using API.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Utilities;
using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static Humanizer.In;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "bao_cao")]
    public class BaoCaoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public BaoCaoController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Báo cáo doanh thu theo bác sĩ chỉ định.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("bc_doanhthu_bscd")]
        public async Task<ActionResult<object>> GetDoanhThuBSCD(BaoCaoRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var doanhthu_bscd = await GetDoanhThuBSCDFunc(req.TuNgay, req.DenNgay, req.MaBacSy, dbData);
                return Ok(new
                {
                    data = doanhthu_bscd,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("bc_doanhthu_bscd_excel")]
        public async Task<IActionResult> GetDoanhThuBSCDExcel([FromBody] BaoCaoRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                if (string.IsNullOrEmpty(csytid))
                    return Unauthorized();
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                // Lấy bệnh viện
                var pCsyTid = new List<DbParameter>();
                var conn = _context.Database.GetDbConnection();
                using (var cmd = conn.CreateCommand())
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@csytid";
                    p.Value = csytid;
                    pCsyTid.Add(p);
                }

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw("SELECT * FROM dmc_benhvien WHERE CSYTID = @csytid", pCsyTid.ToArray())
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var data = await GetDoanhThuBSCDFunc(req.TuNgay, req.DenNgay, req.MaBacSy, dbData);

                using var wb = new XLWorkbook();

                if (data == null || data.Count == 0)
                {
                    var wsEmpty = wb.Worksheets.Add("KET_QUA");
                    wsEmpty.Cell("A1").Value = "Không có dữ liệu";
                    using var msEmpty = new MemoryStream();
                    wb.SaveAs(msEmpty);
                    var fileNameEmpty = $"BCTH_CHI_PHI_BSCD_{req.TuNgay:yyyyMMdd}_{req.DenNgay:yyyyMMdd}.xlsx";
                    return File(msEmpty.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileNameEmpty);
                }

                var doctorGroups = data
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.ten_bacsi) ? "KHONG_RO" : x.ten_bacsi.Trim())
                    .OrderBy(g => g.Key)
                    .ToList();

                var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var doctorGroup in doctorGroups)
                {
                    var sheetName = MakeSafeSheetName(doctorGroup.Key, usedSheetNames);
                    CreateDoctorSheet(
                        wb,
                        sheetName,
                        doctorGroup.ToList(),
                        benhVien?.tenbenhvien ?? "",
                        req.TuNgay,
                        req.DenNgay
                    );
                }

                using var ms = new MemoryStream();
                wb.SaveAs(ms);

                var fileName = $"BCTH_CHI_PHI_BSCD_{req.TuNgay:yyyyMMdd}_{req.DenNgay:yyyyMMdd}.xlsx";
                return File(
                    ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        private void CreateDoctorSheet(
            XLWorkbook wb,
            string sheetName,
            List<API.DTO.BcDoanhThuBscdDto> data,
            string tenBenhVien,
            DateTime tuNgay,
            DateTime denNgay)
        {
            var ws = wb.Worksheets.Add(sheetName);

            ws.Range("A1:J1").Merge();
            ws.Cell("A1").Value = tenBenhVien;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell("A1").Style.Font.Bold = true;

            ws.Range("A2:J2").Merge();
            ws.Cell("A2").Value = "Phòng Tài chính - Kế toán";
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell("A2").Style.Font.Bold = true;

            ws.Range("A3:J3").Merge();
            ws.Cell("A3").Value = BuildTitle(tuNgay, denNgay);
            ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A3").Style.Font.Bold = true;
            ws.Cell("A3").Style.Font.FontSize = 14;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Blue;

            ws.Range("A4:J4").Merge();
            ws.Cell("A4").Value = $"Bác sỹ: {(data.FirstOrDefault()?.ten_bacsi ?? "")}";
            ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A4").Style.Font.Bold = true;

            int row = 6;

            string[] headers =
            {
                "STT",
                "Nội dung",
                "Số lượt",
                "Giá theo quy định",
                "Thành tiền",
                "Chi phí vật tư, hóa chất",
                "Số tiền còn lại",
                "Hệ số",
                "Điểm thực hiện",
                "Ghi chú"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
            }

            var headerRange = ws.Range(row, 1, row, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            headerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            row++;

            var groups = data
                .OrderBy(x => x.nhom_mabhyt_id)
                .ThenBy(x => x.ma_dich_vu)
                .GroupBy(x => x.tennhom)
                .ToList();

            int groupIndex = 0;

            decimal tongAllThanhTien = 0;
            decimal tongAllChiPhiVattu = 0;
            decimal tongAllConLai = 0;
            decimal tongAllDiem = 0;

            foreach (var group in groups)
            {
                groupIndex++;

                ws.Cell(row, 1).Value = groupIndex;
                ws.Range(row, 2, row, 10).Merge();
                ws.Cell(row, 2).Value = group.Key ?? "";
                ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(row, 1, row, 10).Style.Alignment.WrapText = true;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                row++;

                int itemIndex = 0;
                decimal tongThanhTien = 0;
                decimal tongChiPhiVattu = 0;
                decimal tongConLai = 0;
                decimal tongDiem = 0;

                foreach (var item in group)
                {
                    itemIndex++;

                    ws.Cell(row, 1).Value = $"{groupIndex}.{itemIndex}";
                    ws.Cell(row, 2).Value = item.ten_dich_vu ?? "";
                    ws.Cell(row, 3).Value = item.soluong ?? 0;
                    ws.Cell(row, 4).Value = item.don_gia_bh ?? 0;
                    ws.Cell(row, 5).Value = item.thanh_tien ?? 0;
                    ws.Cell(row, 6).Value = item.chiphi_vattu ?? 0;
                    ws.Cell(row, 7).Value = item.sotien_conlai ?? 0;
                    ws.Cell(row, 8).Value = item.heso ?? 0;
                    ws.Cell(row, 9).Value = item.diem_thuchien ?? 0;
                    ws.Cell(row, 10).Value = "";

                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    tongThanhTien += item.thanh_tien ?? 0;
                    tongChiPhiVattu += item.chiphi_vattu ?? 0;
                    tongConLai += item.sotien_conlai ?? 0;
                    tongDiem += item.diem_thuchien ?? 0;

                    tongAllThanhTien += item.thanh_tien ?? 0;
                    tongAllChiPhiVattu += item.chiphi_vattu ?? 0;
                    tongAllConLai += item.sotien_conlai ?? 0;
                    tongAllDiem += item.diem_thuchien ?? 0;

                    row++;
                }

                ws.Cell(row, 2).Value = "Tổng";
                ws.Cell(row, 5).Value = tongThanhTien;
                ws.Cell(row, 6).Value = tongChiPhiVattu;
                ws.Cell(row, 7).Value = tongConLai;
                ws.Cell(row, 9).Value = tongDiem;

                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            ws.Cell(row, 2).Value = "Tổng cộng";
            ws.Cell(row, 5).Value = tongAllThanhTien;
            ws.Cell(row, 6).Value = tongAllChiPhiVattu;
            ws.Cell(row, 7).Value = tongAllConLai;
            ws.Cell(row, 9).Value = tongAllDiem;

            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

            ws.Range(row, 1, row, 10).Style.Font.Bold = true;
            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
            ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.SheetView.FreezeRows(6);

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 55;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 18;
            ws.Column(7).Width = 16;
            ws.Column(8).Width = 10;
            ws.Column(9).Width = 14;
            ws.Column(10).Width = 12;

            ws.Row(1).Height = 22;
            ws.Row(2).Height = 22;
            ws.Row(3).Height = 24;
            ws.Row(4).Height = 22;
            ws.Row(6).Height = 34;

            var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), 10);
            usedRange.Style.Font.FontName = "Times New Roman";
            usedRange.Style.Font.FontSize = 11;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static string MakeSafeSheetName(string name, HashSet<string> usedNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Sheet";

            name = Regex.Replace(name, @"[\[\]\:\*\?\/\\]", " ");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            if (name.Length > 31)
                name = name.Substring(0, 31).Trim();

            var baseName = name;
            int i = 1;

            while (usedNames.Contains(name))
            {
                var suffix = $"_{i++}";
                var maxLen = 31 - suffix.Length;
                var prefix = baseName.Length > maxLen ? baseName.Substring(0, maxLen) : baseName;
                name = prefix + suffix;
            }

            usedNames.Add(name);
            return name;
        }

        private async Task<List<BcDoanhThuBscdDto>> GetDoanhThuBSCDFunc(DateTime tuNgay, DateTime denNgay, string? maBacSi, string dbName)
        {
            var sql = $@"
                            SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, TEN_BACSI, DON_GIA_BH, HESO, CHIPHI, SOLUONG , CHIPHI * SOLUONG AS CHIPHI_VATTU, DON_GIA_BH * SOLUONG AS THANH_TIEN, ((DON_GIA_BH - CHIPHI) * SOLUONG) AS SOTIEN_CONLAI, HESO * SOLUONG AS DIEM_THUCHIEN
                            FROM (
                                SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI, SUM(SO_LUONG) SOLUONG FROM (
                                    SELECT 
                                        nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH, 0) DON_GIA_BH ,IFNULL(dv.HESO,0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI, org.OFFICER_NAME TEN_BACSI
                                    FROM  
                                        `{dbName}`.xml1 a, 
                                        `{dbName}`.xml3 b LEFT JOIN dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU,
                                        dmc_nhom_mabhyt nhom,
                                        org_officer org
                                    WHERE a.ma_lk = b.ma_lk
                                    AND b.ma_nhom = nhom.MANHOM_BHYT
                                    AND b.MA_BAC_SI = org.MA_BAC_SI
                                    AND a.NGAY_RA >= @tungay 
                                    AND a.NGAY_RA <= @dengay 
                                    AND b.ma_bac_si = @maBacSi
                                ) th
                                group by NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI
                            ) th2
                            ORDER BY NHOM_MABHYT_ID, MA_DICH_VU;";

            var conn = _context.Database.GetDbConnection();
            using var tempCmd = conn.CreateCommand();

            var paramList = new List<DbParameter>();

            var p1 = tempCmd.CreateParameter();
            p1.ParameterName = "@tungay";
            p1.Value = tuNgay.Date;
            paramList.Add(p1);

            var p2 = tempCmd.CreateParameter();
            p2.ParameterName = "@dengay";
            p2.Value = denNgay.Date;
            paramList.Add(p2);

            var p3 = tempCmd.CreateParameter();
            p3.ParameterName = "@maBacSi";
            p3.Value = string.IsNullOrWhiteSpace(maBacSi)
                ? DBNull.Value
                : maBacSi.Trim();
            paramList.Add(p3);

            var doanhthu_bscd = await _context.dto_bc_doanhthu_bscd
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();
            return doanhthu_bscd;
        }

        /// <summary>
        /// Báo cáo doanh thu theo bác sĩ thực hiện.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("bc_doanhthu_bsth")]
        public async Task<ActionResult<object>> GetDoanhThuBSTH(BaoCaoRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var doanhthu_bscd = await GetDoanhThuBsthFunc(req.TuNgay, req.DenNgay, req.MaBacSy, dbData);

                return Ok(new
                {
                    data = doanhthu_bscd,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("bc_doanhthu_bsth_excel")]
        public async Task<IActionResult> GetDoanhThuBSTHExcel([FromBody] BaoCaoRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                var sql_tenbv = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var data = await GetDoanhThuBsthFunc(req.TuNgay, req.DenNgay, req.MaBacSy, dbData);

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("BSTH");

                // ====== 4 dòng đầu ======
                ws.Range("A1:J1").Merge();
                ws.Cell("A1").Value = benhVien?.tenbenhvien;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A1").Style.Font.Bold = true;

                ws.Range("A2:J2").Merge();
                ws.Cell("A2").Value = "Phòng Tài chính - Kế toán";
                ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A2").Style.Font.Bold = true;

                ws.Range("A3:J3").Merge();
                ws.Cell("A3").Value = BuildTitle(req.TuNgay, req.DenNgay);
                ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A3").Style.Font.Bold = true;
                ws.Cell("A3").Style.Font.FontSize = 14;
                ws.Cell("A3").Style.Font.FontColor = XLColor.Blue;

                ws.Range("A4:J4").Merge();
                ws.Cell("A4").Value = $"Bác sỹ: {(data.FirstOrDefault()?.ten_bacsi ?? "")}";
                ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A4").Style.Font.Bold = true;

                // Dòng 5 trống
                int row = 6;

                // ====== Header ======
                string[] headers =
                {
                    "STT",
                    "Nội dung",
                    "Số lượt",
                    "Giá theo quy định",
                    "Thành tiền",
                    "Chi phí vật tư, hóa chất",
                    "Số tiền còn lại",
                    "Hệ số",
                    "Điểm thực hiện",
                    "Ghi chú"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = headers[i];
                }

                var headerRange = ws.Range(row, 1, row, 10);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Alignment.WrapText = true;
                headerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Dữ liệu theo nhóm TENNHOM ======
                var groups = data
                    .OrderBy(x => x.nhom_mabhyt_id)
                    .ThenBy(x => x.ma_dich_vu)
                    .GroupBy(x => x.tennhom)
                    .ToList();

                int groupIndex = 0;

                decimal tongAllThanhTien = 0;
                decimal tongAllChiPhiVattu = 0;
                decimal tongAllConLai = 0;
                decimal tongAllDiem = 0;

                foreach (var group in groups)
                {
                    groupIndex++;

                    // Dòng tên nhóm: cột STT + merge 9 cột còn lại
                    ws.Cell(row, 1).Value = groupIndex;
                    ws.Range(row, 2, row, 10).Merge();
                    ws.Cell(row, 2).Value = group.Key ?? "";
                    ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Range(row, 1, row, 10).Style.Alignment.WrapText = true;

                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                    ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    row++;

                    int itemIndex = 0;

                    decimal tongThanhTien = 0;
                    decimal tongChiPhiVattu = 0;
                    decimal tongConLai = 0;
                    decimal tongDiem = 0;

                    foreach (var item in group)
                    {
                        itemIndex++;

                        ws.Cell(row, 1).Value = $"{groupIndex}.{itemIndex}";
                        ws.Cell(row, 2).Value = item.ten_dich_vu ?? "";
                        ws.Cell(row, 3).Value = item.soluong ?? 0;
                        ws.Cell(row, 4).Value = item.don_gia_bh ?? 0;
                        ws.Cell(row, 5).Value = item.thanh_tien ?? 0;
                        ws.Cell(row, 6).Value = item.chiphi_vattu ?? 0;
                        ws.Cell(row, 7).Value = item.sotien_conlai ?? 0;
                        ws.Cell(row, 8).Value = item.heso ?? 0;
                        ws.Cell(row, 9).Value = item.diem_thuchien ?? 0;
                        ws.Cell(row, 10).Value = "";

                        // Căn lề
                        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        // Định dạng số
                        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                        ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        tongThanhTien += item.thanh_tien ?? 0;
                        tongChiPhiVattu += item.chiphi_vattu ?? 0;
                        tongConLai += item.sotien_conlai ?? 0;
                        tongDiem += item.diem_thuchien ?? 0;

                        tongAllThanhTien += item.thanh_tien ?? 0;
                        tongAllChiPhiVattu += item.chiphi_vattu ?? 0;
                        tongAllConLai += item.sotien_conlai ?? 0;
                        tongAllDiem += item.diem_thuchien ?? 0;

                        ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        row++;
                    }
                    // ===== Dòng tổng =====
                    ws.Cell(row, 2).Value = "Tổng";

                    ws.Cell(row, 5).Value = tongThanhTien;
                    ws.Cell(row, 6).Value = tongChiPhiVattu;
                    ws.Cell(row, 7).Value = tongConLai;
                    ws.Cell(row, 9).Value = tongDiem;

                    // format
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                    ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }

                // ===== Dòng tổng cộng toàn bộ =====
                ws.Cell(row, 2).Value = "Tổng cộng";

                ws.Cell(row, 5).Value = tongAllThanhTien;
                ws.Cell(row, 6).Value = tongAllChiPhiVattu;
                ws.Cell(row, 7).Value = tongAllConLai;
                ws.Cell(row, 9).Value = tongAllDiem;

                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
                ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Style chung ======
                ws.SheetView.FreezeRows(6);

                ws.Column(1).Width = 8;
                ws.Column(2).Width = 55;
                ws.Column(3).Width = 10;
                ws.Column(4).Width = 16;
                ws.Column(5).Width = 16;
                ws.Column(6).Width = 18;
                ws.Column(7).Width = 16;
                ws.Column(8).Width = 10;
                ws.Column(9).Width = 14;
                ws.Column(10).Width = 12;

                ws.Row(1).Height = 22;
                ws.Row(2).Height = 22;
                ws.Row(3).Height = 24;
                ws.Row(4).Height = 22;
                ws.Row(6).Height = 34;

                var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), 10);
                usedRange.Style.Font.FontName = "Times New Roman";
                usedRange.Style.Font.FontSize = 11;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                var fileName = $"BCTH_CHI_PHI_BSTH_{req.TuNgay:yyyyMMdd}_{req.DenNgay:yyyyMMdd}.xlsx";

                return File(
                    ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        private async Task<List<BcDoanhThuBscdDto>> GetDoanhThuBsthFunc(DateTime tuNgay, DateTime denNgay, string? maBacSi, string dbName)
        {
            var sql = $@"
                    SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SOLUONG,
                        CHIPHI * SOLUONG AS CHIPHI_VATTU,
                        DON_GIA_BH * SOLUONG AS THANH_TIEN,
                        ((DON_GIA_BH - CHIPHI) * SOLUONG) AS SOTIEN_CONLAI,
                        HESO * SOLUONG AS DIEM_THUCHIEN,
                        bs.TEN_BACSI
                    FROM (
                        SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, IFNULL(DON_GIA_BH,0) DON_GIA_BH, IFNULL(HESO,0) HESO, IFNULL(CHIPHI, 0) CHIPHI, IFNULL(SUM(SO_LUONG),0) SOLUONG
                        FROM (
                            SELECT 
                                nhom.NHOM_MABHYT_ID,
                                IFNULL(b.MA_DICH_VU, b.MA_VAT_TU) MA_DICH_VU,
                                IFNULL(b.TEN_DICH_VU, b.TEN_VAT_TU) TEN_DICH_VU,
                                nhom.TENNHOM,
                                b.SO_LUONG,
                                b.DON_GIA_BH,
                                dv.HESO,
                                dv.CHIPHI
                            FROM  
                                `{dbName}`.xml1 a, 
                                `{dbName}`.xml3 b 
                                LEFT JOIN dmc_dichvu dv 
                                    ON IFNULL(b.MA_DICH_VU, b.MA_VAT_TU) = dv.MA_DICHVU 
                                AND IFNULL(b.TEN_DICH_VU, b.TEN_VAT_TU) = dv.TEN_DICHVU,
                                dmc_nhom_mabhyt nhom
                            WHERE a.ma_lk = b.ma_lk
                            AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND a.NGAY_RA >= @tungay 
                            AND a.NGAY_RA <= @dengay 
                            AND b.nguoi_thuc_hien LIKE @nguoiThucHien
                        ) th
                        GROUP BY NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI
                    ) th2,
                    (select OFFICER_NAME TEN_BACSI FROM org_officer where MA_BAC_SI=@nguoiThucHienCode) bs
                    ORDER BY NHOM_MABHYT_ID, MA_DICH_VU;";

            var conn = _context.Database.GetDbConnection();
            using var tempCmd = conn.CreateCommand();
            var paramList = new List<DbParameter>();

            var p1 = tempCmd.CreateParameter();
            p1.ParameterName = "@tungay";
            p1.Value = tuNgay.Date;
            paramList.Add(p1);

            var p2 = tempCmd.CreateParameter();
            p2.ParameterName = "@dengay";
            p2.Value = denNgay.Date;
            paramList.Add(p2);

            var p3 = tempCmd.CreateParameter();
            p3.ParameterName = "@nguoiThucHien";
            p3.Value =$"%{maBacSi.ToString()}%";
            paramList.Add(p3);

            var p4 = tempCmd.CreateParameter();
            p4.ParameterName = "@nguoiThucHienCode";
            p4.Value = maBacSi.ToString();
            paramList.Add(p4);
            var query = _context.dto_bc_doanhthu_bscd.FromSqlRaw(sql, paramList.ToArray()).ToQueryString();
            return await _context.dto_bc_doanhthu_bscd.FromSqlRaw(sql, paramList.ToArray())
                .AsNoTracking()
                    .ToListAsync();
        }
        [Authorize]
        [HttpPost("bc_doanhthu_khoa")]
        public async Task<ActionResult<object>> GetDoanhThuKhoa(BaoCaoKhoaRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                var sql = $@"
                            SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA, TENNHOM,MA_DICH_VU, TEN_DICH_VU, SOLUONG, DON_GIA_BH, HESO, CHIPHI, SOLUONG * DON_GIA_BH AS THANH_TIEN, CHIPHI * SOLUONG AS CHIPHI_VATTU, SOLUONG * (DON_GIA_BH - CHIPHI) AS SOTIEN_CONLAI , HESO * SOLUONG AS DIEM_THUCHIEN
                    FROM (
	                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SUM(SO_LUONG) SOLUONG FROM (
		                    SELECT 
			                    nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH,0) DON_GIA_BH ,IFNULL(dv.HESO, 0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI
		                    FROM  
			                    his_data_binhluc.xml1 a, 
			                    his_data_binhluc.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU,
			                    his_common.dmc_nhom_mabhyt nhom,
                                his_common.org_organization khoa
		                    WHERE a.ma_lk = b.ma_lk
		                    AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND b.MA_KHOA = khoa.MA_KHOA
		                    AND a.NGAY_RA BETWEEN @tungay AND @denngay
                            AND (b.MA_KHOA = @maKhoa OR @maKhoa=@default)
	                    ) th
	                    group by NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI
                    ) th2
                    ORDER BY MA_KHOA,NHOM_MABHYT_ID, TEN_DICH_VU;";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@denngay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                var p3 = tempCmd.CreateParameter();
                p3.ParameterName = "@maKhoa";
                var mk = (req.MaKhoa == null || req.MaKhoa.Equals("")) ? "-1" : req.MaKhoa.ToString();
                p3.Value = mk;
                paramList.Add(p3);

                var p4 = tempCmd.CreateParameter();
                p4.ParameterName = "@default";
                p4.Value = "-1";
                paramList.Add(p4);

                var doanhthu_bscd = await _context.dto_bc_doanhthu_khoa
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                // using var cmd = _context.Database.GetDbConnection().CreateCommand();
                // cmd.CommandText = @"cmd";
                // await _context.Database.OpenConnectionAsync();

                // using var reader = await cmd.ExecuteReaderAsync();

                // for (int i = 0; i < reader.FieldCount; i++)
                // {
                //     Console.WriteLine($"{reader.GetName(i)} - {reader.GetFieldType(i)}");
                // }

                return Ok(new
                {
                    data = doanhthu_bscd,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("bc_doanhthu_khoa_excel")]
        public async Task<IActionResult> GetDoanhThuKhoaExcel([FromBody] BaoCaoKhoaRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var sql_tenbv = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var sql = @"
                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA, TENNHOM,MA_DICH_VU, TEN_DICH_VU, SOLUONG, DON_GIA_BH, HESO, CHIPHI, SOLUONG * DON_GIA_BH AS THANH_TIEN, CHIPHI * SOLUONG AS CHIPHI_VATTU, SOLUONG * (DON_GIA_BH - CHIPHI) AS SOTIEN_CONLAI , HESO * SOLUONG AS DIEM_THUCHIEN
                    FROM (
	                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SUM(SO_LUONG) SOLUONG FROM (
		                    SELECT 
			                    nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH,0) DON_GIA_BH ,IFNULL(dv.HESO, 0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI
		                    FROM  
			                    his_data_binhluc.xml1 a, 
			                    his_data_binhluc.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU,
			                    his_common.dmc_nhom_mabhyt nhom,
                                his_common.org_organization khoa
		                    WHERE a.ma_lk = b.ma_lk
		                    AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND b.MA_KHOA = khoa.MA_KHOA
		                    AND a.NGAY_RA BETWEEN @tungay AND @denngay
                            AND (b.MA_KHOA = @maKhoa OR @maKhoa=@default)
	                    ) th
	                    group by NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI
                    ) th2
                    ORDER BY MA_KHOA,NHOM_MABHYT_ID, TEN_DICH_VU;";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();
                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@denngay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                var p3 = tempCmd.CreateParameter();
                p3.ParameterName = "@maKhoa";
                var mk = (req.MaKhoa == null || req.MaKhoa.Equals("")) ? "-1" : req.MaKhoa.ToString();
                p3.Value = mk;
                paramList.Add(p3);

                var p4 = tempCmd.CreateParameter();
                p4.ParameterName = "@default";
                p4.Value = "-1";
                paramList.Add(p4);

                var data = await _context.dto_bc_doanhthu_khoa
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                var isShowGroupInOrg = req.isShowGroupInOrg;
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Khoa");

                // ====== 4 dòng đầu ======
                ws.Range("A1:J1").Merge();
                ws.Cell("A1").Value = benhVien?.tenbenhvien;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A1").Style.Font.Bold = true;

                ws.Range("A2:J2").Merge();
                ws.Cell("A2").Value = "Phòng Tài chính - Kế toán";
                ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A2").Style.Font.Bold = true;

                ws.Range("A3:J3").Merge();
                ws.Cell("A3").Value = BuildTitle(req.TuNgay, req.DenNgay);
                ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A3").Style.Font.Bold = true;
                ws.Cell("A3").Style.Font.FontSize = 14;
                ws.Cell("A3").Style.Font.FontColor = XLColor.Blue;

                ws.Range("A4:J4").Merge();
                var orgName = (mk == "-1") ? "Tất cả" : data.FirstOrDefault()?.khoa;
                ws.Cell("A4").Value = $"Khoa: {orgName}";
                ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A4").Style.Font.Bold = true;

                // Dòng 5 trống
                int row = 6;

                // ====== Header ======
                string[] headers =
                {
                    "STT",
                    "Nội dung",
                    "Số lượt",
                    "Giá theo quy định",
                    "Thành tiền",
                    "Chi phí vật tư, hóa chất",
                    "Số tiền còn lại",
                    "Hệ số",
                    "Điểm thực hiện",
                    "Ghi chú"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = headers[i];
                }

                var headerRange = ws.Range(row, 1, row, 10);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Alignment.WrapText = true;
                headerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Dữ liệu theo nhóm KHOA ======
                var orgs = data
                    .OrderBy(x => x.nhom_mabhyt_id)
                    .ThenBy(x => x.ma_dich_vu)
                    .GroupBy(x => x.khoa)
                    .ToList();

                int orgIndex = 0;

                decimal tongAllChiPhiVattu = 0;
                decimal tongAllThanhTien = 0;
                decimal tongAllDiem = 0;
                decimal tongAllSoTienConLai = 0;

                foreach (var org in orgs)
                {
                    orgIndex++;

                    // Dòng tên khoa: cột STT + merge 9 cột còn lại
                    ws.Cell(row, 1).Value = orgIndex;
                    ws.Range(row, 2, row, 10).Merge();
                    ws.Cell(row, 2).Value = org.Key ?? "";
                    ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Range(row, 1, row, 10).Style.Alignment.WrapText = true;

                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                    ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    row++;

                    decimal tongChiPhiVattuOrg = 0;
                    decimal tongDiemOrg = 0;
                    decimal tongThanhTienOrg = 0;
                    decimal tongSoTienConLaiOrg = 0;

                    // trong 1 khoa, nhóm lại theo tên nhóm
                    var groups = org.GroupBy(g => g.tennhom).ToList();
                    var groupIndex = 0;
                    var totalGrItemIndex = 0;
                    foreach (var group in groups)
                    {
                        groupIndex++;
                        if (isShowGroupInOrg)
                        {
                            // group gồm nhiều dịch vụ
                            // Dòng tên nhóm: cột STT + merge 9 cột còn lại
                            ws.Cell(row, 1).Value = $"{orgIndex}.{groupIndex}";
                            ws.Range(row, 2, row, 10).Merge();
                            ws.Cell(row, 2).Value = group.Key ?? "";
                            ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                            ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            ws.Range(row, 1, row, 10).Style.Alignment.WrapText = true;

                            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                            ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            row++;
                        }
                        decimal itemIndex = 0;
                        decimal tongThanhTienGroup = 0;
                        decimal tongChiPhiGroup = 0;
                        decimal tongDiemGroup = 0;
                        decimal tongSoTienConLaiGroup = 0;

                        foreach (var item in group)
                        {
                            itemIndex++;
                            totalGrItemIndex++;
                            ws.Cell(row, 1).Value = isShowGroupInOrg ? $"{orgIndex}.{groupIndex}.{itemIndex}" : $"{orgIndex}.{totalGrItemIndex}";
                            ws.Cell(row, 2).Value = item.ten_dich_vu ?? "";
                            ws.Cell(row, 3).Value = item.soluong ?? 0;
                            ws.Cell(row, 4).Value = item.don_gia_bh ?? 0;
                            ws.Cell(row, 5).Value = item.thanh_tien ?? 0;
                            ws.Cell(row, 6).Value = item.chiphi_vattu ?? 0;
                            ws.Cell(row, 7).Value = item.sotien_conlai ?? 0;
                            ws.Cell(row, 8).Value = item.heso ?? 0;
                            ws.Cell(row, 9).Value = item.diem_thuchien ?? 0;
                            ws.Cell(row, 10).Value = "";

                            // Căn lề
                            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            // Định dạng số
                            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                            ws.Range(row, 1, row, 10).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            ws.Range(row, 1, row, 10).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            tongChiPhiGroup += item.chiphi_vattu ?? 0;
                            tongDiemGroup += item.diem_thuchien ?? 0;
                            tongSoTienConLaiGroup += item.sotien_conlai ?? 0;
                            tongThanhTienGroup += item.thanh_tien ?? 0;
                            ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            row++;
                        }
                        if (isShowGroupInOrg)
                        {
                            // ===== Dòng tổng theo nhóm =====
                            ws.Cell(row, 2).Value = "Tổng theo nhóm";
                            ws.Cell(row, 5).Value = tongThanhTienGroup;
                            ws.Cell(row, 6).Value = tongChiPhiGroup;
                            ws.Cell(row, 7).Value = tongSoTienConLaiGroup;
                            ws.Cell(row, 9).Value = tongDiemGroup;

                            // format
                            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                            ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                            ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            row++;
                        }

                        tongThanhTienOrg += tongThanhTienGroup;
                        tongChiPhiVattuOrg += tongChiPhiGroup;
                        tongDiemOrg += tongDiemGroup;
                        tongSoTienConLaiOrg += tongSoTienConLaiGroup;
                    }
                    // ===== Dòng tổng theo khoa =====
                    ws.Cell(row, 2).Value = "Tổng theo khoa";
                    ws.Cell(row, 5).Value = tongThanhTienOrg;
                    ws.Cell(row, 6).Value = tongChiPhiVattuOrg;
                    ws.Cell(row, 7).Value = tongSoTienConLaiOrg;
                    ws.Cell(row, 9).Value = tongDiemOrg;

                    // format
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                    ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;

                    tongAllThanhTien += tongThanhTienOrg;
                    tongAllChiPhiVattu += tongChiPhiVattuOrg;
                    tongAllDiem += tongDiemOrg;
                    tongAllSoTienConLai += tongSoTienConLaiOrg;

                    ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                    ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // ===== Dòng tổng cộng toàn bộ =====
                ws.Cell(row, 2).Value = "Tổng";

                ws.Cell(row, 5).Value = tongAllThanhTien;
                ws.Cell(row, 6).Value = tongAllChiPhiVattu;
                ws.Cell(row, 7).Value = tongAllSoTienConLai;
                ws.Cell(row, 9).Value = tongAllDiem;

                // format
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                ws.Range(row, 1, row, 10).Style.Font.Bold = true;
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
                ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Style chung ======
                ws.SheetView.FreezeRows(6);

                ws.Column(1).Width = 5;//
                ws.Column(2).Width = 60;//
                ws.Column(3).Width = 10;//
                ws.Column(4).Width = 15;//
                ws.Column(5).Width = 20;//
                ws.Column(6).Width = 20;//
                ws.Column(7).Width = 20;//
                ws.Column(8).Width = 10;
                ws.Column(9).Width = 10;//
                ws.Column(10).Width = 10;//

                ws.Row(1).Height = 22;
                ws.Row(2).Height = 22;
                ws.Row(3).Height = 24;
                ws.Row(4).Height = 22;
                ws.Row(6).Height = 34;

                var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), 10);
                usedRange.Style.Font.FontName = "Times New Roman";
                usedRange.Style.Font.FontSize = 11;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                var fileName = $"BCTH_CHI_PHI_KHOA_{req.TuNgay:yyyyMMdd}_{req.DenNgay:yyyyMMdd}.xlsx";

                return File(
                    ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("bc_doanhthu_toanvien")]
        public async Task<ActionResult<object>> GetDoanhThuToanVien(BaoCaoKhoaRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                var sql = $@"
                            SELECT MA_KHOA, KHOA,  SUM(THANH_TIEN) THANH_TIEN, SUM(CHIPHI_VATTU) CHIPHI_VATTU, SUM(SOTIEN_CONLAI) SOTIEN_CONLAI
                            FROM (        SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA, TENNHOM,MA_DICH_VU, TEN_DICH_VU, SOLUONG, DON_GIA_BH, HESO, CHIPHI, SOLUONG * DON_GIA_BH AS THANH_TIEN, CHIPHI * SOLUONG AS CHIPHI_VATTU, SOLUONG * (DON_GIA_BH - CHIPHI) AS SOTIEN_CONLAI , HESO * SOLUONG AS DIEM_THUCHIEN
				                            FROM (
					                            SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SUM(SO_LUONG) SOLUONG FROM (
						                            SELECT 
							                            nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH,0) DON_GIA_BH ,IFNULL(dv.HESO, 0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI
						                            FROM  
							                            his_data_binhluc.xml1 a, 
							                            his_data_binhluc.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU,
							                            his_common.dmc_nhom_mabhyt nhom,
							                            his_common.org_organization khoa
						                            WHERE a.ma_lk = b.ma_lk
						                            AND b.ma_nhom = nhom.MANHOM_BHYT
						                            AND b.MA_KHOA = khoa.MA_KHOA
						                            AND a.NGAY_RA BETWEEN @tungay AND @denngay
					                            ) th
					                            group by NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI
				                            ) th2
				                            ORDER BY MA_KHOA,NHOM_MABHYT_ID, TEN_DICH_VU) th3

                            GROUP BY MA_KHOA, KHOA";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@denngay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                //var p3 = tempCmd.CreateParameter();
                //p3.ParameterName = "@maKhoa";
                //var mk = (req.MaKhoa == null || req.MaKhoa.Equals("")) ? "-1" : req.MaKhoa.ToString();
                //p3.Value = mk;
                //paramList.Add(p3);

                //var p4 = tempCmd.CreateParameter();
                //p4.ParameterName = "@default";
                //p4.Value = "-1";
                //paramList.Add(p4);

                var doanhthu_bscd = await _context.dto_bc_doanhthu_toanvien
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                // using var cmd = _context.Database.GetDbConnection().CreateCommand();
                // cmd.CommandText = @"cmd";
                // await _context.Database.OpenConnectionAsync();

                // using var reader = await cmd.ExecuteReaderAsync();

                // for (int i = 0; i < reader.FieldCount; i++)
                // {
                //     Console.WriteLine($"{reader.GetName(i)} - {reader.GetFieldType(i)}");
                // }

                return Ok(new
                {
                    data = doanhthu_bscd,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("bc_doanhthu_toanvien_excel")]
        public async Task<IActionResult> GetDoanhThuToanvienExcel([FromBody] BaoCaoKhoaRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var sql_tenbv = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var sql = @"
                    SELECT MA_KHOA, KHOA,  SUM(THANH_TIEN) THANH_TIEN, SUM(CHIPHI_VATTU) CHIPHI_VATTU, SUM(SOTIEN_CONLAI) SOTIEN_CONLAI
                    FROM (        SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA, TENNHOM,MA_DICH_VU, TEN_DICH_VU, SOLUONG, DON_GIA_BH, HESO, CHIPHI, SOLUONG * DON_GIA_BH AS THANH_TIEN, CHIPHI * SOLUONG AS CHIPHI_VATTU, SOLUONG * (DON_GIA_BH - CHIPHI) AS SOTIEN_CONLAI , HESO * SOLUONG AS DIEM_THUCHIEN
				                    FROM (
					                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SUM(SO_LUONG) SOLUONG FROM (
						                    SELECT 
							                    nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH,0) DON_GIA_BH ,IFNULL(dv.HESO, 0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI
						                    FROM  
							                    his_data_binhluc.xml1 a, 
							                    his_data_binhluc.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU,
							                    his_common.dmc_nhom_mabhyt nhom,
							                    his_common.org_organization khoa
						                    WHERE a.ma_lk = b.ma_lk
						                    AND b.ma_nhom = nhom.MANHOM_BHYT
						                    AND b.MA_KHOA = khoa.MA_KHOA
						                    AND a.NGAY_RA BETWEEN @tungay AND @denngay
					                    ) th
					                    group by NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI
				                    ) th2
				                    ORDER BY MA_KHOA,NHOM_MABHYT_ID, TEN_DICH_VU) th3

                    GROUP BY MA_KHOA, KHOA";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();
                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@denngay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                //var p3 = tempCmd.CreateParameter();
                //p3.ParameterName = "@maKhoa";
                //var mk = (req.MaKhoa == null || req.MaKhoa.Equals("")) ? "-1" : req.MaKhoa.ToString();
                //p3.Value = mk;
                //paramList.Add(p3);

                //var p4 = tempCmd.CreateParameter();
                //p4.ParameterName = "@default";
                //p4.Value = "-1";
                //paramList.Add(p4);

                var data = await _context.dto_bc_doanhthu_toanvien
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                var isShowGroupInOrg = req.isShowGroupInOrg;
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Toàn viện");

                // ====== 4 dòng đầu ======
                ws.Range("A1:G1").Merge();
                ws.Cell("A1").Value = benhVien?.tenbenhvien;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A1").Style.Font.Bold = true;

                ws.Range("A2:G2").Merge();
                ws.Cell("A2").Value = "Phòng Tài chính - Kế toán";
                ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A2").Style.Font.Bold = true;

                ws.Range("A3:G3").Merge();
                ws.Cell("A3").Value = BuildTitle(req.TuNgay, req.DenNgay);
                ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A3").Style.Font.Bold = true;
                ws.Cell("A3").Style.Font.FontSize = 14;
                ws.Cell("A3").Style.Font.FontColor = XLColor.Blue;

                // Dòng 5 trống
                int row = 6;

                // ====== Header ======
                string[] headers =
                {
                    "STT",
                    "Mã khoa",
                    "Tên khoa",
                    "Thành tiền",
                    "Chi phí vật tư, hóa chất",
                    "Số tiền còn lại",
                    "Ghi chú"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = headers[i];
                }

                var headerRange = ws.Range(row, 1, row, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Alignment.WrapText = true;
                headerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== D ======

                int idx = 0;

                decimal tongAllChiPhiVattu = 0;
                decimal tongAllThanhTien = 0;
                decimal tongAllSoTienConLai = 0;
                foreach (var item in data)
                {

                    idx++;
                    ws.Cell(row, 1).Value = $"{idx}";
                    ws.Cell(row, 2).Value = item.ma_khoa ?? "";
                    ws.Cell(row, 3).Value = item.khoa ?? "";
                    ws.Cell(row, 4).Value = item.thanh_tien ?? 0;
                    ws.Cell(row, 5).Value = item.chiphi_vattu ?? 0;
                    ws.Cell(row, 6).Value = item.sotien_conlai ?? 0;
                    ws.Cell(row, 7).Value = "";

                    // Căn lề
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    // Định dạng số
                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, headers.Length).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, headers.Length).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    tongAllChiPhiVattu += item.chiphi_vattu ?? 0;
                    tongAllSoTienConLai += item.sotien_conlai ?? 0;
                    tongAllThanhTien += item.thanh_tien ?? 0;
                    ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }


                // ===== Dòng tổng cộng toàn bộ =====
                ws.Cell(row, 2).Value = "Tổng";

                ws.Cell(row, 4).Value = tongAllThanhTien;
                ws.Cell(row, 5).Value = tongAllChiPhiVattu;
                ws.Cell(row, 6).Value = tongAllSoTienConLai;

                // format
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";

                ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Style chung ======
                ws.SheetView.FreezeRows(6);

                ws.Column(1).Width = 5;//
                ws.Column(2).Width = 10;//
                ws.Column(3).Width = 30;//
                ws.Column(4).Width = 20;//
                ws.Column(5).Width = 20;//
                ws.Column(6).Width = 20;//
                ws.Column(7).Width = 10;//

                ws.Row(1).Height = 22;
                ws.Row(2).Height = 22;
                ws.Row(3).Height = 24;
                ws.Row(4).Height = 22;
                ws.Row(6).Height = 34;

                var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), headers.Length);
                usedRange.Style.Font.FontName = "Times New Roman";
                usedRange.Style.Font.FontSize = 11;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                var fileName = $"BCTH_CHI_PHI_TOANVIEN_{req.TuNgay:yyyyMMdd}_{req.DenNgay:yyyyMMdd}.xlsx";

                return File(
                    ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("bc_diem_ctkh")]
        ///<summary>
        ///Lấy ds bc điểm ctkh của khoa v1, k có mặc định là 0 
        ///phiên bản mới: bác sĩ có thể chuyển khoa nên khi lấy điểm ctkh gán bác sĩ và khoa linh động => join theo bacsiid, khoaid của bảng diemkehoach
        ///bác sĩ/điều dưỡng chưa nhập điểm thì k có trong báo cáo
        ///</summary>
        public async Task<IActionResult> GetBcDiemCtkh([FromBody] BaoCaoDiemCtkhRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                // khơi tạo arrThangNam để thêm vào điều kiện lọc theo trường ThangNam
                var endPointMonth = Math.Max((req.DenNam - req.TuNam) * 12, req.DenThang);
                var arrThangNam = new List<int>();
                for (var year = req.TuNam; year <= req.DenNam; year++)
                {
                    for (var month = req.TuThang; month <= endPointMonth; month++)
                    {
                        var tempM = month % 12 == 0 ? 12 : month % 12;
                        arrThangNam.Add(Convert.ToInt32($"{tempM}{year}"));
                    }
                }
                // tạo fromDate, toDate để thêm vào điều kiện lọc Điểm CĐ
                var fromDate = new DateTime(req.TuNam, req.TuThang, 1);
                var endDate = new DateTime(req.DenNam, req.DenThang, DateTime.DaysInMonth(req.DenNam, req.DenThang));


                var dsDiemCtkh = await GetBcDiemCtkhFunc(fromDate, endDate, arrThangNam, dbData);
                return Ok(new {
                    data = dsDiemCtkh,
                    message = "Lấy ds điểm CTKH Thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi server: " + ex.Message);
            }
        }
        private async Task<List<DiemCtkh>> GetBcDiemCtkhFunc(DateTime tuNgay, DateTime denNgay, List<int>? arrThangNam, string dbName)
        {

            var sql = $"SELECT officer.OFFICER_NAME, officer.OFFICER_TYPE, officer.BACSIID, t1.DIEMTHUCHIEN, t2.KHOAID, t2.DIEM_KEHOACH, org.ORG_NAME KHOA, t2.DIEM_TRUC, t2.DIEMTANGCUONG, t2.SONGAYTANGCUONG, t3.DIEMCDNHAPVIEN*6.4 DIEMCDNHAPVIEN, t3.DIEMCDNHAPVIENBNND*19.2 DIEMCDNHAPVIENBNND, t1.DIEMPTTCD*0.2 DIEMPTTCHIDINH, t5.DIEMPTTTHUCHIEN*0.8 DIEMPTTTHUCHIEN, t4.DIEMBANT  FROM " +
                $"(select dkh.KHOAID, IFNULL(sum(dkh.DIEM_KEHOACH), 0) as DIEM_KEHOACH, dkh.BACSIID, IFNULL(sum(dkh.DIEM_TRUC), 0) as DIEM_TRUC, IFNULL(sum(tcSum.DIEMTANGCUONG), 0) as DIEMTANGCUONG, IFNULL(sum(tcSum.SONGAYTANGCUONG), 0) as SONGAYTANGCUONG  from `{dbName}`.bc_diemkehoach dkh " +
                $"LEFT JOIN ( SELECT DIEMKEHOACHID, IFNULL(SUM(tc.DIEM),0) DIEMTANGCUONG, IFNULL(SUM(tc.SONGAY),0) SONGAYTANGCUONG " +
                $"FROM `{dbName}`.bc_tangcuong tc " +
                $"GROUP BY DIEMKEHOACHID ) tcSum " +
                $"ON  tcSum.DIEMKEHOACHID= dkh.DIEMKEHOACHID WHERE THANGNAM in (" + GenerateDynamicParamThangNam(arrThangNam) + ") " +
                $"GROUP BY KHOAID , BACSIID) t2 " +
                $"LEFT JOIN  (SELECT t.DIEMTHUCHIEN, activeUsers.BACSIID, t.DIEMPTTCD " +
                $"FROM (SELECT  MA_BAC_SI, SUM(HESO) as DIEMTHUCHIEN, SUM(CASE WHEN NHOM_MABHYT_ID IN(6,26) THEN HESO ELSE 0 END ) DIEMPTTCD  " +
                $"FROM (SELECT  " +
                $"nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH, 0) DON_GIA_BH ,IFNULL(dv.HESO,0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI, b.MA_BAC_SI " +
                $"FROM   `{dbName}`.xml1 a,  `{dbName}`.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU, " +
                $"his_common.dmc_nhom_mabhyt nhom " +
                $"WHERE a.ma_lk = b.ma_lk " +
                $"AND b.ma_nhom = nhom.MANHOM_BHYT " +
                $"AND a.NGAY_RA >= @tuNgay " +
                $"AND a.NGAY_RA <= @denNgay ) x  " +
                $"group by MA_BAC_SI) t " +
                $"LEFT JOIN (SELECT * from his_common.org_officer WHERE STATUS = 1 AND MA_BAC_SI IS NOT NULL AND MA_BAC_SI <> \"\") activeUsers  " +
                $"ON t.MA_BAC_SI = activeUsers.MA_BAC_SI) t1 " +
                $"ON t2.BACSIID = t1.BACSIID " +
                $"LEFT JOIN ( SELECT BACSIID, SUM(CASE WHEN BHYT = 1 THEN SOLUONG ELSE 0 END) DIEMCDNHAPVIEN, SUM(CASE WHEN BHYT = 2 THEN SOLUONG ELSE 0 END) DIEMCDNHAPVIENBNND FROM `{dbName}`.bc_benhnhan_nhapvien WHERE THANGNAM IN (" + GenerateDynamicParamThangNam(arrThangNam) + ")  " +
                $"GROUP BY BACSIID ) t3 " +
                $"ON t2.BACSIID = t3.BACSIID " +
                $"LEFT JOIN ( SELECT BACSIID, SUM(SOLUONG) DIEMBANT FROM `{dbName}`.bc_benhnhan_15t WHERE THANGNAM IN (" + GenerateDynamicParamThangNam(arrThangNam) + ") AND BHYT = 1 " +
                $"GROUP BY BACSIID ) t4 " +
                $"ON t2.BACSIID = t4.BACSIID " +
                $"LEFT JOIN  (SELECT pttTHSum.*,  org.BACSIID, org.OFFICER_NAME " +
                $"FROM  (SELECT NGUOI_THUC_HIEN, SUM(SO_LUONG*HESO) DIEMPTTTHUCHIEN  " +
                $"FROM (SELECT tPtt.NHOM_MABHYT_ID, tPtt.SO_LUONG, tPtt.HESO,jt.NGUOI_THUC_HIEN " +
                $"FROM (SELECT nhom.NHOM_MABHYT_ID,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(dv.HESO,0) HESO, b.NGUOI_THUC_HIEN " +
                $"FROM  `{dbName}`.xml1 a, " +
                $"`{dbName}`.xml3 b " +
                $"LEFT JOIN his_common.dmc_dichvu dv " +
                $"on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU, " +
                $"his_common.dmc_nhom_mabhyt nhom " +
                $"WHERE a.ma_lk = b.ma_lk " +
                $"AND b.ma_nhom = nhom.MANHOM_BHYT " +
                $"AND a.NGAY_RA >= @tuNgay " +
                $"AND a.NGAY_RA <= @denNgay AND nhom.NHOM_MABHYT_ID IN (6,26) ) tPtt " +
                $"JOIN JSON_TABLE( " +
                $"CONCAT('[\"', REPLACE(tPtt.NGUOI_THUC_HIEN, ';', '\",\"'), '\"]'), " +
                $"'$[*]' COLUMNS( NGUOI_THUC_HIEN VARCHAR(255) PATH '$' )) AS jt) tPttSplit GROUP BY NGUOI_THUC_HIEN) pttTHSum " +
                $"LEFT JOIN his_common.org_officer org " +
                $"ON org.MA_BAC_SI = pttTHSum.NGUOI_THUC_HIEN) t5 " +
                $"ON t5.BACSIID = t2.BACSIID " +
                $"LEFT JOIN his_common.org_organization org ON org.ORG_ID = t2.KHOAID " +
                $"LEFT JOIN his_common.org_officer officer ON officer.BACSIID = t2.BACSIID " +
                $"order by t2.KHOAID;";
            var conn = _context.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            var paramList = new List<DbParameter>();
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@tuNgay";
            p1.Value = tuNgay;
            paramList.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@denNgay";
            p2.Value = denNgay;
            paramList.Add(p2);

            for(var i=0; i< arrThangNam.Count;i++)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = $"pThangNam{i}";
                p.Value = arrThangNam[i];
                paramList.Add(p);
            }
            var query = _context.diemCtkhs.FromSqlRaw(sql, paramList.ToArray());
            var qryStr = query.ToQueryString();
            Console.WriteLine(qryStr);
            return await query
                    .AsNoTracking()
                    .ToListAsync();
        }
        private string GenerateDynamicParamThangNam(List<int>? arrThangNam)
        {
            var res = "";
            for(var i=0; i< arrThangNam.Count; i++)
            {
                if (i > 0) res += ",";
                res += $"@pThangNam{i}";
            }
            return res;
        }
        [Authorize]
        [HttpPost("bc_diem_ctkh_excel")]
        public async Task<IActionResult> GetBcDiemCtkhExcel([FromBody] BaoCaoDiemCtkhRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                // khơi tạo arrThangNam để thêm vào điều kiện lọc theo trường ThangNam
                var endPointMonth = Math.Max((req.DenNam - req.TuNam) * 12, req.DenThang);
                var arrThangNam = new List<int>();
                for (var year = req.TuNam; year <= req.DenNam; year++)
                {
                    for (var month = req.TuThang; month <= endPointMonth; month++)
                    {
                        var tempM = month % 12 == 0 ? 12 : month % 12;
                        arrThangNam.Add(Convert.ToInt32($"{tempM}{year}"));
                    }
                }
                // tạo fromDate, toDate để thêm vào điều kiện lọc Điểm CĐ
                var fromDate = new DateTime(req.TuNam, req.TuThang, 1);
                var endDate = new DateTime(req.DenNam, req.DenThang, DateTime.DaysInMonth(req.DenNam, req.DenThang));
                var dsBcDiemCtkh = await GetBcDiemCtkhFunc(fromDate, endDate, arrThangNam, dbData);

                var sql_tenbv = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                var res = await GenerateBcDtkhExcel(dsBcDiemCtkh, req, benhVien);
                using var ms = new MemoryStream();
                
                res.SaveAs(ms);
                var fileName = $"BC_DIEM_CTKH_{Enum.GetName(typeof(LoaiBaoCaoCtkh), req.LoaiBaoCao)}{fromDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

                return File(
                    ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            } catch (Exception ex)
            {
                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
        }

        private async Task<XLWorkbook> GenerateBcDtkhExcel(List<DiemCtkh>? dataRaw, BaoCaoDiemCtkhRequest req, BenhVien? benhVien)
        {
            try
            {
                if (req.LoaiBaoCao == LoaiBaoCaoCtkh.BAC_SI) return GenerateBcCtkhExcelBacSi(dataRaw, req, benhVien);
                else if (req.LoaiBaoCao == LoaiBaoCaoCtkh.DIEU_DUONG) return GenerateBcCtkhExcelDieuDuong(dataRaw, req, benhVien);
                else return null;
            }catch(Exception ex) {
                return null;
            }
        }
        public enum ReportRowType
        {
            GROUP,
            ITEM,
            GRAND_TOTAL
        }
        public class ReportCtkhRow
        {
            public ReportRowType type { get; set; }
            public string stt { get; set; }
            public DiemCtkh DiemCtkh { get; set; }

            public decimal tongCong { get; set; }
            public decimal diemTHTheoBS { get; set; }
            public decimal datCtkh { get; set; }
        }
        private string ConvertToRoman(int num)
        {
            var lookup = new List<KeyValuePair<string, int>>
        {
            new("M", 1000), new("CM", 900),
            new("D", 500),  new("CD", 400),
            new("C", 100),  new("XC", 90),
            new("L", 50),   new("XL", 40),
            new("X", 10),   new("IX", 9),
            new("V", 5),    new("IV", 4),
            new("I", 1)
        };

            string roman = "";
            foreach (var (key, value) in lookup)
            {
                while (num >= value)
                {
                    roman += key;
                    num -= value;
                }
            }

            return roman;
        }


        public class CtkhHeaderCell
        {
            public string row1 { get; set; }
            public string[] row2 { get; set; }
            public string[] row3 { get; set; }
        }
        private XLWorkbook GenerateBcCtkhExcelBacSi(List<DiemCtkh>? dataRaw, BaoCaoDiemCtkhRequest req, BenhVien? benhVien) {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Bác sĩ");
            string[] colName = new string[30];
            int colCount = 20;
            int idx = 1;
            colName[0] = "";
            for(char c='A'; c <'Z'; c++)
            {
                colName[idx++] = c.ToString();

            }
            int row = 1;
            //// ====== 4 dòng đầu ======
            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            ws.Cell(row, 1).Value = benhVien?.tenbenhvien;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            var txtLoaiBc = req.LoaiBaoCao == LoaiBaoCaoCtkh.BAC_SI ? "BÁC SỸ" : "ĐIỀU DƯỠNG";
            ws.Cell(row, 1).Value = $"BẢNG TỔNG HỢP {txtLoaiBc} - DƯỢC SỸ THỰC HIỆN CHỈ TIÊU KẾ HOẠCH KCB";
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Blue;
            row++;

            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            ws.Cell(row, 1).Value = BuildTitleBcCtkhExcel(req.TuThang, req.TuNam, req.DenThang, req.DenNam);
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Blue;

            // Dòng 4 trống
            row += 2; // trống 1 dòng
            
            //// ====== Header ======
            CtkhHeaderCell[] headers =
            [
                    new CtkhHeaderCell{row1="STT", row2 = [], row3 = ["A"]},
                    new CtkhHeaderCell{row1="Khoa, bác sỹ",row2 = [], row3 = ["B"]} ,
                    new CtkhHeaderCell{row1="Điểm kế hoạch", row2=[], row3=["1"]} ,
                    new CtkhHeaderCell{row1="Điểm CĐ Khám, điều trị, phát thuốc (Dược)",row2 = [], row3 = ["2"]} ,
                    new CtkhHeaderCell{row1="Điểm CĐ nhập viện, Dược(hc)",row2 = [], row3 = ["3"]} ,
                    new CtkhHeaderCell{row1="Phẫu, thủ thuật",row2 = ["Chỉ địnhx0.2", "Thực hiệnx0.8"], row3 = ["4","5"]} ,
                    new CtkhHeaderCell{row1="Điểm BH/ĐT; Tăng cường",row2=[], row3=["6"]},
                    new CtkhHeaderCell{row1="Trực",row2=[], row3=["7"]},
                    new CtkhHeaderCell{row1="Điểm cộng BA ngoại trú; TH siêu âm, Dược",row2=[], row3=["8"]},
                    new CtkhHeaderCell{row1="Điểm TH PTT theo Điều dưỡng",row2=[], row3=["9"]},
                    new CtkhHeaderCell{row1="Điểm BNND",row2=["Chỉ định","Thực hiện","Điểm CĐ nhập viện, Dược(hc)"], row3=["10","11","12"]},
                    new CtkhHeaderCell{row1="Tổng điểm",row2=[],row3=["13 = 2+...+12"]},
                    new CtkhHeaderCell{row1="Khoa chia lại điểm",row2=[],row3=["14"]},
                    new CtkhHeaderCell{row1="Đạt CTKH Bác sỹ",row2=[], row3=["15=13/1"]},
                    new CtkhHeaderCell{row1="Tổng cộng chung cả khoa",row2 = ["Điểm K.H", "Tổng điểm T.H", "Đạt CTKH"],row3=["31=1+16", "32=13+28", "33=32/31"]}
            ];
            int hCol = 1;
            for (int i = 0; i < headers.Length; i++)
            {
                int row2Length = headers[i].row2.Length;
                if (row2Length > 0)
                {
                    // có hàng 2, hàng 3
                    ws.Range($"{colName[hCol]}{row}:{colName[hCol+row2Length-1]}{row}").Merge(); // merge chiều ngang ~ colspan
                    ws.Cell($"{colName[hCol]}{row}").Value = headers[i].row1;
                    for(int j = 0; j<row2Length; j++)
                    {
                        // vẽ hàng 2, 3
                        ws.Cell($"{colName[hCol+j]}{row+1}").Value = headers[i].row2[j];
                        ws.Cell($"{colName[hCol+j]}{row+2}").Value = headers[i].row3[j];
                    }
                    hCol += row2Length;
                }
                else
                {
                    // không có hàng 2, thì hàng 1 có rowspan = 2;
                    ws.Range($"{colName[hCol]}{row}:{colName[hCol]}{row+1}").Merge(); // merge chiều dọc ~ rowspan
                    ws.Cell($"{colName[hCol]}{row}").Value = headers[i].row1;
                    ws.Cell($"{colName[hCol]}{row + 2}").Value = headers[i].row3[0];
                    hCol += 1;
                }
            }

            var header1Range = ws.Range(row, 1, row+1, colCount);
            header1Range.Style.Font.Bold = true;
            header1Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header1Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header1Range.Style.Alignment.WrapText = true;
            header1Range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            var header2Range = ws.Range(row+2, 1, row + 2, colCount);
            header2Range.Style.Font.Italic = true;
            header2Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header2Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header2Range.Style.Alignment.WrapText = true;
            header2Range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            row += 3;

            //// ====== D ======

            // ====== Dữ liệu theo nhóm KhoaId ======
            var filteredData = new List<DiemCtkh>();
            foreach(var d in dataRaw)
            {
                if(d.OfficerType == 4) filteredData.Add(d);
            }
            var groups = filteredData
                .GroupBy(x => x.Khoa)
                .ToList();

            int groupIndex = 0;

            for (int gCount = 0; gCount < groups.Count; gCount++)
            {
                var group = groups[gCount];
                groupIndex++;

                // Dòng tên nhóm: cột STT + merge 9 cột còn lại
                var tongBs = group.Count();
                ws.Range(row, 1, row, colCount).Style.Font.Bold = true;
                ws.Range(row, 1, row, colCount).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(row, 1, row, colCount).Style.Alignment.WrapText = true;

                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                for(int col = 1; col<= colCount; col++)
                {
                    if(col==1) ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    else if(col!= 2) ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    
                }

                ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                ws.Cell(row,1).Value = ConvertToRoman(gCount + 1);
                ws.Cell(row, 2).Value = group.Key ?? "";
                ws.Cell(row, 3).FormulaA1 = $"SUM({colName[3]}{row + 1}:{colName[3]}{row + tongBs})";
                ws.Cell(row, 4).FormulaA1 = $"SUM({colName[4]}{row + 1}:{colName[4]}{row + tongBs})";
                ws.Cell(row, 5).FormulaA1 = $"SUM({colName[5]}{row + 1}:{colName[5]}{row + tongBs})";
                ws.Cell(row, 6).FormulaA1 = $"SUM({colName[6]}{row + 1}:{colName[6]}{row + tongBs})";
                ws.Cell(row, 7).FormulaA1 = $"SUM({colName[7]}{row + 1}:{colName[7]}{row + tongBs})";
                ws.Cell(row, 8).FormulaA1 = $"SUM({colName[8]}{row + 1}:{colName[8]}{row + tongBs})";
                ws.Cell(row, 9).FormulaA1 = $"SUM({colName[9]}{row + 1}:{colName[9]}{row + tongBs})";
                ws.Cell(row, 10).FormulaA1 = $"SUM({colName[10]}{row + 1}:{colName[10]}{row + tongBs})";
                ws.Cell(row, 14).FormulaA1 = $"SUM({colName[14]}{row + 1}:{colName[14]}{row + tongBs})";
                ws.Cell(row, 15).FormulaA1 = $"SUM({colName[15]}{row + 1}:{colName[15]}{row + tongBs})";
                ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row} ,0)";
                ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
                ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";
                row++;
                int itemIndex = 0;

                foreach (var item in group)
                {
                    itemIndex++;

                    ws.Cell(row, 1).Value = $"{itemIndex}";
                    var type = item.OfficerType == 4 ? "BS:" : "ĐD:";
                    ws.Cell(row, 2).Value = $"{type}{item.OfficerName ?? ""}";
                    ws.Cell(row, 3).Value = item.DiemKeHoach ?? 0;
                    ws.Cell(row, 4).Value = item.DiemCdKham ?? 0;
                    ws.Cell(row, 5).Value = item.DiemCDDieuTri ?? 0;
                    ws.Cell(row, 6).Value = item.DiemPTTCD ?? 0;
                    ws.Cell(row, 7).Value = item.DiemPTTTH ?? 0;
                    ws.Cell(row, 8).Value = item.DiemTangCuong ?? 0;
                    ws.Cell(row, 9).Value = item.DiemTruc ?? 0;
                    ws.Cell(row, 10).Value =item.DiemCongBANT ?? 0;
                    ws.Cell(row, 14).Value =item.DiemBNNDCDNhapVien ?? 0;
                    ws.Cell(row, 15).FormulaA1 = $"SUM({colName[4]}{row}:{colName[14]}{row})"; // tổng các cột trước
                    ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row},0)"; // tính %
                    ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
                    // Căn lề
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Định dạng số
                    ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Range(row, 1, row, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }
            }

            // ===== Dòng tổng cộng toàn bộ =====
            ws.Cell(row, 2).Value = "Tổng cộng";
            ws.Range(row, 1, row, colCount).Style.Font.Bold = true;
            ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
            ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(row,3).FormulaA1 = $"SUM({colName[3]}{8}:{colName[3]}{row-1})/2";
            ws.Cell(row, 4).FormulaA1 = $"SUM({colName[4]}{8}:{colName[4]}{row-1})/2";
            ws.Cell(row, 5).FormulaA1 = $"SUM({colName[5]}{8}:{colName[5]}{row-1})/2";
            ws.Cell(row, 6).FormulaA1 = $"SUM({colName[6]}{8}:{colName[6]}{row-1})/2";
            ws.Cell(row, 7).FormulaA1 = $"SUM({colName[7]}{8}:{colName[7]}{row-1})/2";
            ws.Cell(row, 8).FormulaA1 = $"SUM({colName[8]}{8}:{colName[8]}{row-1})/2";
            ws.Cell(row, 9).FormulaA1 = $"SUM({colName[9]}{8}:{colName[9]}{row-1})/2";
            ws.Cell(row, 10).FormulaA1 = $"SUM({colName[10]}{8}:{colName[10]}{row-1})/2";
            ws.Cell(row, 14).FormulaA1 = $"SUM({colName[14]}{8}:{colName[14]}{row-1})/2";
            ws.Cell(row, 15).FormulaA1 = $"SUM({colName[15]}{8}:{colName[15]}{row-1})/2";
            ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row} ,0)";
            ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
            ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";
            row++;

            // ====== Style chung ======
            ws.SheetView.FreezeRows(5);

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 40;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 12;
            ws.Column(8).Width = 12;
            ws.Column(9).Width = 12;
            ws.Column(10).Width = 14;
            ws.Column(11).Width = 14;
            ws.Column(12).Width = 10;
            ws.Column(13).Width = 10;
            ws.Column(14).Width = 10;
            ws.Column(15).Width = 20;
            ws.Column(16).Width = 16;
            ws.Column(17).Width = 16;
            ws.Column(18).Width = 16;
            ws.Column(19).Width = 16;
            ws.Column(20).Width = 16;


            ws.Row(1).Height = 22;
            ws.Row(2).Height = 24;
            ws.Row(3).Height = 24;
            ws.Row(4).Height = 22;
            ws.Row(5).Height = 50;

            var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), colCount);
            usedRange.Style.Font.FontName = "Times New Roman";
            usedRange.Style.Font.FontSize = 11;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            return wb;
        }
        private XLWorkbook GenerateBcCtkhExcelDieuDuong(List<DiemCtkh>? dataRaw, BaoCaoDiemCtkhRequest req, BenhVien? benhVien) {
            var wb = new XLWorkbook();
            
            var ws = wb.Worksheets.Add("Điều dưỡng");
            string[] colName = new string[30];
            int colCount = 17;
            int idx = 1;
            colName[0] = "";
            for (char c = 'A'; c < 'Z'; c++)
            {
                colName[idx++] = c.ToString();

            }
            int row = 1;
            //// ====== 4 dòng đầu ======
            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            ws.Cell(row, 1).Value = benhVien?.tenbenhvien;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            var txtLoaiBc = req.LoaiBaoCao == LoaiBaoCaoCtkh.BAC_SI ? "BÁC SỸ" : "ĐIỀU DƯỠNG";
            ws.Cell(row, 1).Value = $"BẢNG TỔNG HỢP {txtLoaiBc} - DƯỢC SỸ THỰC HIỆN CHỈ TIÊU KẾ HOẠCH KCB";
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Blue;
            row++;

            ws.Range($"{colName[1]}{row}:{colName[colCount]}{row}").Merge();
            ws.Cell(row, 1).Value = BuildTitleBcCtkhExcel(req.TuThang, req.TuNam, req.DenThang, req.DenNam);
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Blue;

            // Dòng 4 trống
            row += 2; // trống 1 dòng

            //// ====== Header ======
            CtkhHeaderCell[] headers = [
                new CtkhHeaderCell{row1="STT", row2 = [], row3 = ["C"]},
                    new CtkhHeaderCell{row1="Điều dưỡng",row2 = [], row3 = ["D"]} ,
                    new CtkhHeaderCell{row1="Điểm kế hoạch", row2=[], row3=["16"]} ,
                    new CtkhHeaderCell{row1="Điểm T.H tại khoa theo Bs, Dược (phát thuốc)",row2 = [], row3 = ["17=2+...+6\r\n=2+...+5(TN)"]} ,
                    new CtkhHeaderCell{row1="Đi tăng cường",row2 = ["Số ngày","Tổng điểm"], row3 = ["18","19"]} ,
                    new CtkhHeaderCell{row1="Khoa được tăng cường",row2 = ["Số ngày được tc", "Điểm trừ"], row3 = ["20","21"]} ,
                    new CtkhHeaderCell{row1="Điểm lấy mẫu máu, nước tiểu, Dược (hc)",row2=[], row3=["22"]},
                    new CtkhHeaderCell{row1="Điểm cộng theo Bs; Đ.tim (CLS); Dược",row2=[], row3=["23"]},
                    new CtkhHeaderCell{row1="Trực",row2=[], row3=["24"]},
                    new CtkhHeaderCell{row1="Điểm TH thủ thuật 1 Điều dưỡng",row2=[], row3=["25"]},
                    new CtkhHeaderCell{row1="Điểm BNND",row2=["Theo Bs", "BN nhập viện"], row3=["26","27"]},
                    new CtkhHeaderCell{row1="Tổng điểm",row2=[],row3=["28=17+...+27"]},
                    new CtkhHeaderCell{row1="Khoa chia lại điểm",row2=[],row3=["29"]},
                    new CtkhHeaderCell{row1="Đạt CTKH Điều dưỡng",row2=[], row3=["30=28/16"]},
            ];
            int hCol = 1;
            for (int i = 0; i < headers.Length; i++)
            {
                int row2Length = headers[i].row2.Length;
                if (row2Length > 0)
                {
                    // có hàng 2, hàng 3
                    ws.Range($"{colName[hCol]}{row}:{colName[hCol + row2Length - 1]}{row}").Merge(); // merge chiều ngang ~ colspan
                    ws.Cell($"{colName[hCol]}{row}").Value = headers[i].row1;
                    for (int j = 0; j < row2Length; j++)
                    {
                        // vẽ hàng 2, 3
                        ws.Cell($"{colName[hCol + j]}{row + 1}").Value = headers[i].row2[j];
                        ws.Cell($"{colName[hCol + j]}{row + 2}").Value = headers[i].row3[j];
                    }
                    hCol += row2Length;
                }
                else
                {
                    // không có hàng 2, thì hàng 1 có rowspan = 2;
                    ws.Range($"{colName[hCol]}{row}:{colName[hCol]}{row + 1}").Merge(); // merge chiều dọc ~ rowspan
                    ws.Cell($"{colName[hCol]}{row}").Value = headers[i].row1;
                    ws.Cell($"{colName[hCol]}{row + 2}").Value = headers[i].row3[0];
                    hCol += 1;
                }
            }

            var header1Range = ws.Range(row, 1, row + 1, colCount);
            header1Range.Style.Font.Bold = true;
            header1Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header1Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header1Range.Style.Alignment.WrapText = true;
            header1Range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header1Range.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            var header2Range = ws.Range(row + 2, 1, row + 2, colCount);
            header2Range.Style.Font.Italic = true;
            header2Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header2Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header2Range.Style.Alignment.WrapText = true;
            header2Range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header2Range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            row += 3;

            //// ====== D ======

            // ====== Dữ liệu theo nhóm KhoaId ======
            var filteredData = new List<DiemCtkh>();
            decimal diemTongTHBS = 0;
            foreach (var d in dataRaw)
            {
                if (d.OfficerType == 6) filteredData.Add(d);
            }
            var groups = filteredData
                .GroupBy(x => x.Khoa)
                .ToList();

            int groupIndex = 0;
            
            for (int gCount = 0; gCount < groups.Count; gCount++)
            {
                var group = groups[gCount];
                groupIndex++;
                decimal diemTHTheoBSKhoa = 0;
                foreach(var nv in dataRaw)
                {
                    if(nv.OfficerType == 4 && nv.Khoa==group.Key)
                    {
                        diemTHTheoBSKhoa = diemTHTheoBSKhoa +
                            (nv.DiemCdKham ?? 0m) + (nv.DiemCDDieuTri ?? 0m) + (nv.DiemPTTCD ?? 0m) + (nv.DiemPTTTH ?? 0m) + (nv.DiemTangCuong ?? 0m) + (nv.DiemTruc ?? 0m) + (nv.DiemCongBANT ?? 0m)
                            + (nv.DiemBNNDCDNhapVien ?? 0m);
                    }
                }
                // Dòng tên nhóm: cột STT + merge 9 cột còn lại
                var tongDd = group.Count();
                ws.Range(row, 1, row, colCount).Style.Font.Bold = true;
                ws.Range(row, 1, row, colCount).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(row, 1, row, colCount).Style.Alignment.WrapText = true;

                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                for (int col = 1; col <= colCount; col++)
                {
                    if (col == 1) ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    else if (col != 2) ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                }

                ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, 1).Value = ConvertToRoman(gCount + 1);
                ws.Cell(row, 2).Value = group.Key ?? "";
                ws.Cell(row, 3).FormulaA1 = $"SUM({colName[3]}{row + 1}:{colName[3]}{row + tongDd})";
                ws.Cell(row, 4).FormulaA1 = $"SUM({colName[4]}{row + 1}:{colName[4]}{row + tongDd})";
                ws.Cell(row, 5).FormulaA1 = $"SUM({colName[5]}{row + 1}:{colName[5]}{row + tongDd})";
                ws.Cell(row, 6).FormulaA1 = $"SUM({colName[6]}{row + 1}:{colName[6]}{row + tongDd})";
                
                ws.Cell(row, 11).FormulaA1 = $"SUM({colName[11]}{row + 1}:{colName[11]}{row + tongDd})";
                ws.Cell(row, 15).FormulaA1 = $"SUM({colName[15]}{row + 1}:{colName[15]}{row + tongDd})";
                ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row} ,0)";
                ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
                ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";
                row++;
                int itemIndex = 0;

                foreach (var item in group)
                {
                    itemIndex++;

                    ws.Cell(row, 1).Value = $"{itemIndex}";
                    var type = item.OfficerType == 4 ? "BS:" : "ĐD:";
                    ws.Cell(row, 2).Value = $"{type}{item.OfficerName ?? ""}";
                    ws.Cell(row, 3).Value = item.DiemKeHoach ?? 0;
                    ws.Cell(row, 4).Value = diemTHTheoBSKhoa / tongDd;
                    ws.Cell(row, 5).Value = item.SoNgayTangCuong ?? 0;
                    ws.Cell(row, 6).Value = item.DiemTangCuong ?? 0;
                    
                    ws.Cell(row, 11).Value = item.DiemTruc ?? 0;
                    ws.Cell(row, 15).FormulaA1 = $"SUM({colName[4]}{row}:{colName[14]}{row})-{colName[5]}{row}-{colName[7]}{row}"; // tổng các cột trước
                    ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row},0)"; // tính %
                    ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
                    // Căn lề
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Định dạng số
                    ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, colCount).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Range(row, 1, row, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }
            }

            // ===== Dòng tổng cộng toàn bộ =====
            ws.Cell(row, 2).Value = "Tổng cộng";
            ws.Range(row, 1, row, colCount).Style.Font.Bold = true;
            ws.Range(row, 1, row, colCount).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
            ws.Range(row, 1, row, colCount).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, colCount).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, 3).FormulaA1 = $"SUM({colName[3]}{8}:{colName[3]}{row - 1})/2";
            ws.Cell(row, 4).FormulaA1 = $"SUM({colName[4]}{8}:{colName[4]}{row - 1})/2";
            ws.Cell(row, 5).FormulaA1 = $"SUM({colName[5]}{8}:{colName[5]}{row - 1})/2";
            ws.Cell(row, 6).FormulaA1 = $"SUM({colName[6]}{8}:{colName[6]}{row - 1})/2";
           
            ws.Cell(row, 11).FormulaA1 = $"SUM({colName[11]}{8}:{colName[11]}{row - 1})/2";
            ws.Cell(row, 15).FormulaA1 = $"SUM({colName[15]}{8}:{colName[15]}{row - 1})/2";
            ws.Cell(row, 17).FormulaA1 = $"IF({colName[3]}{row} > 0 ,{colName[15]}{row}/{colName[3]}{row} ,0)";
            ws.Cell(row, 17).Style.NumberFormat.Format = "0.00%";
            ws.Range($"{colName[3]}{row}:{colName[15]}{row}").Style.NumberFormat.Format = "#,##0.##";
            row++;

            // ====== Style chung ======
            ws.SheetView.FreezeRows(5);

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 40;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 12;
            ws.Column(8).Width = 12;
            ws.Column(9).Width = 12;
            ws.Column(10).Width = 14;
            ws.Column(11).Width = 14;
            ws.Column(12).Width = 10;
            ws.Column(13).Width = 10;
            ws.Column(14).Width = 10;
            ws.Column(15).Width = 20;
            ws.Column(16).Width = 16;
            ws.Column(17).Width = 16;
           


            ws.Row(1).Height = 22;
            ws.Row(2).Height = 24;
            ws.Row(3).Height = 24;
            ws.Row(4).Height = 22;
            ws.Row(5).Height = 50;

            var usedRange = ws.Range(1, 1, Math.Max(row - 1, 6), colCount);
            usedRange.Style.Font.FontName = "Times New Roman";
            usedRange.Style.Font.FontSize = 11;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            return wb;
        }

        private string BuildTitleBcCtkhExcel(int tuThang, int tuNam, int denThang, int denNam)
        {
            if (denNam == tuNam && (denThang - tuThang) <= 5)
            {
                string temp1 = "";
                for (int i = tuThang; i <= denThang; i++)
                {
                    temp1 = temp1 + $" {i} " + "+";
                }
                temp1 = temp1.Substring(0, temp1.Length - 1);
                return $" THÁNG {temp1} NĂM {tuNam}";
            }
            else
            {
               return $" TỪ THÁNG {tuThang} NĂM {tuNam} ĐẾN THÁNG {denThang} NĂM {denNam}";
            }
        }
        [Authorize]
        [HttpPost("bc_diem_ctkh_default0")]
        ///<summary>
        ///Lấy ds bc điểm ctkh của khoa v1, k có mặc định là 0 
        ///phiên bản cũ: 1 bác sĩ chỉ ở 1 khoa và k có chuyển khoa
        ///</summary>
        public async Task<IActionResult> GetBcDiemCtkhDefaul0([FromBody] BaoCaoDiemCtkhRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                // khơi tạo arrThangNam để thêm vào điều kiện lọc theo trường ThangNam
                var endPointMonth = Math.Max((req.DenNam - req.TuNam) * 12, req.DenThang);
                var arrThangNam = new List<string>();
                for (var year = req.TuNam; year <= req.DenNam; year++)
                {
                    for (var month = req.TuThang; month <= endPointMonth; month++)
                    {
                        var tempM = month % 12 == 0 ? 12 : month % 12;
                        arrThangNam.Add($"{tempM}{year}");
                    }
                }
                // tạo fromDate, toDate để thêm vào điều kiện lọc Điểm CĐ
                var fromDate = new DateTime(req.TuNam, req.TuThang, 1);
                var endDate = new DateTime(req.DenNam, req.DenThang, DateTime.DaysInMonth(req.DenNam, req.DenThang));
                var sql = $"SELECT  t1.*, t2.DIEMKEHOACHID, t2.DIEM_KEHOACH, t2.DIEM_TRUC, t2.DIEMTANGCUONG, t2.SONGAYTANGCUONG, t3.DIEMCDNHAPVIEN, t1.DIEMTHUCHIEN*0.2 DIEMPTTCHIDINH, t1.DIEMTHUCHIEN*0.8 DIEMPTTTHUCHIEN FROM (SELECT activeUsers.OFFICER_TYPE, activeUsers.OFFICER_NAME, activeUsers.BACSIID, activeUsers.KHOAID, org.ORG_NAME KHOA, t.DIEMTHUCHIEN " +
                    $"FROM (SELECT * from his_common.org_officer WHERE STATUS = 1 AND MA_BAC_SI IS NOT NULL AND MA_BAC_SI <> '' ) activeUsers " +
                    $"LEFT JOIN (SELECT  MA_BAC_SI, SUM(HESO) as DIEMTHUCHIEN " +
                    $"FROM (SELECT " +
                    $"nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH, 0) DON_GIA_BH ,IFNULL(dv.HESO,0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI, b.MA_BAC_SI" +
                    $" FROM " +
                    $"`{dbData}`.xml1 a, " +
                    $"`{dbData}`.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU, " +
                    $"his_common.dmc_nhom_mabhyt nhom " +
                    $"WHERE a.ma_lk = b.ma_lk " +
                    $"AND b.ma_nhom = nhom.MANHOM_BHYT " +
                    $"AND a.NGAY_RA >= @tuNgay " +
                    $"AND a.NGAY_RA <= @denNgay) x " +
                    $"group by MA_BAC_SI) t " +
                    $"ON t.MA_BAC_SI = activeUsers.MA_BAC_SI " +
                    $"LEFT JOIN his_common.org_organization org " +
                    $"ON org.ORG_ID = activeUsers.KHOAID) t1 " +
                    $"LEFT JOIN (select dkh.DIEMKEHOACHID, dkh.DIEM_KEHOACH, dkh.BACSIID, dkh.DIEM_TRUC, IFNULL(sum(tc.DIEM),0) as DIEMTANGCUONG,IFNULL(sum(tc.SONGAY),0) as SONGAYTANGCUONG from `{dbData}`.bc_diemkehoach dkh " +
                    $"LEFT JOIN `{dbData}`.bc_tangcuong tc " +
                    $"ON  tc.DIEMKEHOACHID= dkh.DIEMKEHOACHID " +
                    $"WHERE THANGNAM in (@arrThangNam) " +
                    $"GROUP BY DIEMKEHOACHID, DIEM_KEHOACH, BACSIID, DIEM_TRUC) t2 " +
                    $"ON t1.BACSIID = t2.BACSIID " +
                    $"LEFT JOIN ( SELECT BACSIID, SUM(SOLUONG) DIEMCDNHAPVIEN FROM `{dbData}`.bc_benhnhan_nhapvien WHERE THANGNAM IN (@arrThangNam) AND BHYT = 1 " +
                    $"GROUP BY BACSIID " +
                    $") t3 " +
                    $"ON t1.BACSIID = t3.BACSIID " +
                    $"order by KHOAID";
                var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                var paramList = new List<DbParameter>();
                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@tuNgay";
                p1.Value = fromDate;
                paramList.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@denNgay";
                p2.Value = endDate;
                paramList.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.ParameterName = "@arrThangNam";
                p3.Value = string.Join(',', arrThangNam);
                paramList.Add(p3);

                var dsDiemCtkh = await _context.diemCtkhs.FromSqlRaw(sql, paramList.ToArray())
                .AsNoTracking()
                .ToListAsync();
                return Ok(new
                {
                    data = dsDiemCtkh,
                    message = "Lấy ds điểm CTKH Thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi server: " + ex.Message);
            }
        }
        private static string BuildTitle(DateTime tuNgay, DateTime denNgay)
        {
            var months = new List<int>();
            var cursor = new DateTime(tuNgay.Year, tuNgay.Month, 1);
            var end = new DateTime(denNgay.Year, denNgay.Month, 1);

            while (cursor <= end)
            {
                months.Add(cursor.Month);
                cursor = cursor.AddMonths(1);
            }

            var monthText = string.Join(", ", months.Distinct());
            return $"BẢNG TỔNG HỢP CHI PHÍ KHÁM CHỮA BỆNH NỘI – NGOẠI TRÚ BHYT THÁNG {monthText} NĂM {denNgay:yyyy}";
        }

        public class BaoCaoRequest
        {
            public DateTime TuNgay { get; set; }
            public DateTime DenNgay { get; set; }
            public string? MaBacSy { get; set; }
        }

        public class BaoCaoKhoaRequest
        {
            public DateTime TuNgay { get; set; }
            public DateTime DenNgay { get; set; }
            public string? MaKhoa { get; set; }
            public bool isShowGroupInOrg { get; set; }
        }

        public class BaoCaoDiemCtkhRequest
        {
            public int TuThang { get; set; }
            public int TuNam { get; set; }
            public int DenThang { get; set; }
            public int DenNam { get; set; }
            public LoaiBaoCaoCtkh? LoaiBaoCao { get; set; }
        }

        public enum LoaiBaoCaoCtkh {
            BAC_SI,
            DIEU_DUONG
        }

    }
}