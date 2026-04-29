using API.Common;
using API.Data;
using API.DTO;
using API.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
        
        private async Task<List<BcDoanhThuBscdDto>> GetDoanhThuBSCDFunc(DateTime tuNgay, DateTime denNgay, string? maBacSi, string dbName )
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

                var sql = $@"
                            SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, TEN_BACSI, DON_GIA_BH, HESO, CHIPHI, SOLUONG , CHIPHI * SOLUONG AS CHIPHI_VATTU, DON_GIA_BH * SOLUONG AS THANH_TIEN, ((DON_GIA_BH - CHIPHI) * SOLUONG) AS SOTIEN_CONLAI, HESO * SOLUONG AS DIEM_THUCHIEN
                            FROM (
                                SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI, SUM(SO_LUONG) SOLUONG FROM (
                                    SELECT 
                                        nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH, 0) DON_GIA_BH ,IFNULL(dv.HESO,0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI, org.OFFICER_NAME TEN_BACSI
                                    FROM  
                                        his_data_binhluc.xml1 a, 
                                        his_data_binhluc.xml3 b LEFT JOIN dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU,
                                        dmc_nhom_mabhyt nhom,
                                        org_officer org
                                    WHERE a.ma_lk = b.ma_lk
                                    AND b.ma_nhom = nhom.MANHOM_BHYT
                                    AND b.NGUOI_THUC_HIEN = org.MA_BAC_SI
                                    AND a.NGAY_RA >= @tungay 
                                    AND a.NGAY_RA <= @dengay 
                                    AND b.nguoi_thuc_hien = @nguoiThucHien
                                ) th
                                group by NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI
                            ) th2
                            ORDER BY NHOM_MABHYT_ID, MA_DICH_VU;";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@dengay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                var p3 = tempCmd.CreateParameter();
                p3.ParameterName = "@nguoiThucHien";
                p3.Value = req.MaBacSy.ToString();
                paramList.Add(p3);

                var doanhthu_bscd = await _context.dto_bc_doanhthu_bscd
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

                var sql_tenbv = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var sql = @"
                    SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, TEN_BACSI, DON_GIA_BH, HESO, CHIPHI, SOLUONG,
                        CHIPHI * SOLUONG AS CHIPHI_VATTU,
                        DON_GIA_BH * SOLUONG AS THANH_TIEN,
                        ((DON_GIA_BH - CHIPHI) * SOLUONG) AS SOTIEN_CONLAI,
                        HESO * SOLUONG AS DIEM_THUCHIEN
                    FROM (
                        SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, IFNULL(DON_GIA_BH,0) DON_GIA_BH, IFNULL(HESO,0), IFNULL(CHIPHI, 0) CHIPHI, TEN_BACSI, IFNULL(SUM(SO_LUONG),0) SOLUONG
                        FROM (
                            SELECT 
                                nhom.NHOM_MABHYT_ID,
                                IFNULL(b.MA_DICH_VU, b.MA_VAT_TU) MA_DICH_VU,
                                IFNULL(b.TEN_DICH_VU, b.TEN_VAT_TU) TEN_DICH_VU,
                                nhom.TENNHOM,
                                b.SO_LUONG,
                                b.DON_GIA_BH,
                                dv.HESO,
                                dv.CHIPHI,
                                org.OFFICER_NAME TEN_BACSI
                            FROM  
                                his_data_binhluc.xml1 a, 
                                his_data_binhluc.xml3 b 
                                LEFT JOIN dmc_dichvu dv 
                                    ON IFNULL(b.MA_DICH_VU, b.MA_VAT_TU) = dv.MA_DICHVU 
                                AND IFNULL(b.TEN_DICH_VU, b.TEN_VAT_TU) = dv.TEN_DICHVU,
                                dmc_nhom_mabhyt nhom,
                                org_officer org
                            WHERE a.ma_lk = b.ma_lk
                            AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND b.NGUOI_THUC_HIEN = org.MA_BAC_SI
                            AND a.NGAY_RA >= @tungay 
                            AND a.NGAY_RA <= @dengay 
                            AND b.nguoi_thuc_hien = @nguoiThucHien
                        ) th
                        GROUP BY NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI
                    ) th2
                    ORDER BY NHOM_MABHYT_ID, MA_DICH_VU;";

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();
                var paramList = new List<DbParameter>();

                var p1 = tempCmd.CreateParameter();
                p1.ParameterName = "@tungay";
                p1.Value = req.TuNgay.Date;
                paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                p2.ParameterName = "@dengay";
                p2.Value = req.DenNgay.Date;
                paramList.Add(p2);

                var p3 = tempCmd.CreateParameter();
                p3.ParameterName = "@nguoiThucHien";
                p3.Value = req.MaBacSy.ToString();
                paramList.Add(p3);

                var data = await _context.dto_bc_doanhthu_bscd
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

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
                var mk = (req.MaKhoa==null|| req.MaKhoa.Equals("")) ? "-1" : req.MaKhoa.ToString();
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
                var orgName = (mk == "-1") ? "Tất cả" :data.FirstOrDefault()?.khoa;
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

                        foreach(var item in group)
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
                       if(isShowGroupInOrg)
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
                foreach(var item in data)
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
                var endPointMonth =Math.Max((req.DenNam - req.TuNam) * 12, req.DenThang);
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
                var sql = $"SELECT officer.OFFICER_NAME, officer.OFFICER_TYPE, officer.BACSIID, t1.DIEMTHUCHIEN, t2.DIEMKEHOACHID, t2.DIEM_KEHOACH, org.ORG_NAME KHOA, t2.DIEM_TRUC, t2.DIEMTANGCUONG, t2.SONGAYTANGCUONG, t2.KHOAID, t3.DIEMCDNHAPVIEN, t1.DIEMTHUCHIEN*0.2 DIEMPTTCHIDINH, t1.DIEMTHUCHIEN*0.8 DIEMPTTTHUCHIEN, t4.DIEMBANT  FROM " +
                    $"(select dkh.DIEMKEHOACHID, dkh.KHOAID ,dkh.DIEM_KEHOACH, dkh.BACSIID, dkh.DIEM_TRUC, IFNULL(sum(tc.DIEM),0) as DIEMTANGCUONG,IFNULL(sum(tc.SONGAY),0) as SONGAYTANGCUONG  from {dbData}.bc_diemkehoach dkh " +
                    $"LEFT JOIN {dbData}.bc_tangcuong tc " +
                    $"ON  tc.DIEMKEHOACHID= dkh.DIEMKEHOACHID " +
                    $"WHERE THANGNAM in (@arrThangNam) " +
                    $"GROUP BY DIEMKEHOACHID,KHOAID ,DIEM_KEHOACH, BACSIID, DIEM_TRUC) t2 " +
                    $"LEFT JOIN  (SELECT t.DIEMTHUCHIEN, activeUsers.BACSIID " +
                    $"FROM (SELECT  MA_BAC_SI, SUM(HESO) as DIEMTHUCHIEN " +
                    $"FROM (SELECT " +
                    $"nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,IFNULL(b.SO_LUONG,0) SO_LUONG,IFNULL(b.DON_GIA_BH, 0) DON_GIA_BH ,IFNULL(dv.HESO,0) HESO, IFNULL(dv.CHIPHI,0) CHIPHI, b.MA_BAC_SI " +
                    $"FROM  {dbData}.xml1 a, {dbData}.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU, " +
                    $"his_common.dmc_nhom_mabhyt nhom " +
                    $"WHERE a.ma_lk = b.ma_lk AND b.ma_nhom = nhom.MANHOM_BHYT AND a.NGAY_RA >= @tuNgay AND a.NGAY_RA <= @denNgay) x " +
                    $"group by MA_BAC_SI) t " +
                    $"LEFT JOIN (SELECT * from his_common.org_officer WHERE STATUS = 1 AND MA_BAC_SI IS NOT NULL AND MA_BAC_SI <> '') activeUsers " +
                    $"ON t.MA_BAC_SI = activeUsers.MA_BAC_SI ) t1 " +
                    $"ON t2.BACSIID = t1.BACSIID " +
                    $"LEFT JOIN ( SELECT BACSIID, SUM(SOLUONG) DIEMCDNHAPVIEN FROM {dbData}.bc_benhnhan_nhapvien WHERE THANGNAM IN (@arrThangNam) AND BHYT = 1 GROUP BY BACSIID ) t3 ON t2.BACSIID = t3.BACSIID " +
                    $"LEFT JOIN ( SELECT BACSIID, SUM(SOLUONG) DIEMBANT FROM {dbData}.bc_benhnhan_15t WHERE THANGNAM IN (@arrThangNam) AND BHYT = 1 GROUP BY BACSIID ) t4 ON t2.BACSIID = t4.BACSIID " +
                    $"LEFT JOIN his_common.org_organization org ON org.ORG_ID = t2.KHOAID " +
                    $"LEFT JOIN his_common.org_officer officer ON officer.BACSIID = t2.BACSIID " +
                    $"order by t2.KHOAID";
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
                return Ok(new {
                    data = dsDiemCtkh,
                    message="Lấy ds điểm CTKH Thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi server: " + ex.Message);
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
        }

    }
}