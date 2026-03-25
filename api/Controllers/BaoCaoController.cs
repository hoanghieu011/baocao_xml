using API.Common;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

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

                var sql = $@"
                            SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, TEN_BACSI, DON_GIA_BH, HESO, CHIPHI, SOLUONG , CHIPHI * SOLUONG AS CHIPHI_VATTU, DON_GIA_BH * SOLUONG AS THANH_TIEN, ((DON_GIA_BH - CHIPHI) * SOLUONG) AS SOTIEN_CONLAI, HESO * SOLUONG AS DIEM_THUCHIEN
                            FROM (
                                SELECT NHOM_MABHYT_ID,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI, SUM(SO_LUONG) SOLUONG FROM (
                                    SELECT 
                                        nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,b.SO_LUONG,b.DON_GIA_BH ,dv.HESO, dv.CHIPHI, org.OFFICER_NAME TEN_BACSI
                                    FROM  
                                        his_data_binhluc.XML1 a, 
                                        his_data_binhluc.xml3 b LEFT JOIN dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU AND IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) = dv.TEN_DICHVU,
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
                    p1.Value = req.TuNgay.Date;
                    paramList.Add(p1);

                var p2 = tempCmd.CreateParameter();
                    p2.ParameterName = "@dengay";
                    p2.Value = req.DenNgay.Date;
                    paramList.Add(p2);

                var p3 = tempCmd.CreateParameter();
                    p3.ParameterName = "@maBacSi";
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
        [HttpPost("bc_doanhthu_bscd_excel")]
        public async Task<IActionResult> GetDoanhThuBSCDExcel([FromBody] BaoCaoRequest req)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var sql_tenbv = $@"SELECT * FROM DMC_BENHVIEN WHERE CSYTID = {csytid}";

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
                        SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI, SUM(SO_LUONG) SOLUONG
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
                                his_data_binhluc.XML1 a, 
                                his_data_binhluc.xml3 b 
                                LEFT JOIN dmc_dichvu dv 
                                    ON IFNULL(b.MA_DICH_VU, b.MA_VAT_TU) = dv.MA_DICHVU 
                                AND IFNULL(b.TEN_DICH_VU, b.TEN_VAT_TU) = dv.TEN_DICHVU,
                                dmc_nhom_mabhyt nhom,
                                org_officer org
                            WHERE a.ma_lk = b.ma_lk
                            AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND b.MA_BAC_SI = org.MA_BAC_SI
                            AND a.NGAY_RA >= @tungay 
                            AND a.NGAY_RA <= @dengay 
                            AND b.ma_bac_si = @maBacSi
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
                p3.ParameterName = "@maBacSi";
                p3.Value = req.MaBacSy.ToString();
                paramList.Add(p3);

                var data = await _context.dto_bc_doanhthu_bscd
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("BSCĐ");

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
                                        nhom.NHOM_MABHYT_ID,IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) MA_DICH_VU,IFNULL(b.TEN_DICH_VU,b.TEN_VAT_TU) TEN_DICH_VU,nhom.TENNHOM,b.SO_LUONG,b.DON_GIA_BH ,dv.HESO, dv.CHIPHI, org.OFFICER_NAME TEN_BACSI
                                    FROM  
                                        his_data_binhluc.XML1 a, 
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

                var sql_tenbv = $@"SELECT * FROM DMC_BENHVIEN WHERE CSYTID = {csytid}";

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
                        SELECT NHOM_MABHYT_ID, MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, TEN_BACSI, SUM(SO_LUONG) SOLUONG
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
                                his_data_binhluc.XML1 a, 
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
			                    nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,b.SO_LUONG,b.DON_GIA_BH ,dv.HESO, dv.CHIPHI
		                    FROM  
			                    his_data_binhluc.XML1 a, 
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

                var sql_tenbv = $@"SELECT * FROM DMC_BENHVIEN WHERE CSYTID = {csytid}";

                var benhVien = await _context.dmc_benhvien
                    .FromSqlRaw(sql_tenbv)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                var sql = @"
                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA, TENNHOM,MA_DICH_VU, TEN_DICH_VU, SOLUONG, DON_GIA_BH, HESO, CHIPHI, SOLUONG * DON_GIA_BH AS THANH_TIEN, CHIPHI * SOLUONG AS CHIPHI_VATTU, SOLUONG * (DON_GIA_BH - CHIPHI) AS SOTIEN , HESO * SOLUONG AS DIEM_THUCHIEN
                    FROM (
	                    SELECT NHOM_MABHYT_ID,MA_KHOA,KHOA,MA_DICH_VU, TEN_DICH_VU, TENNHOM, DON_GIA_BH, HESO, CHIPHI, SUM(SO_LUONG) SOLUONG FROM (
		                    SELECT 
			                    nhom.NHOM_MABHYT_ID,b.MA_KHOA,khoa.ORG_NAME KHOA,dv.MA_DICHVU MA_DICH_VU,dv.TEN_DICHVU TEN_DICH_VU,nhom.TENNHOM,b.SO_LUONG,b.DON_GIA_BH ,dv.HESO, dv.CHIPHI
		                    FROM  
			                    his_data_binhluc.XML1 a, 
			                    his_data_binhluc.xml3 b LEFT JOIN his_common.dmc_dichvu dv on IFNULL(b.MA_DICH_VU,b.MA_VAT_TU) = dv.MA_DICHVU,
			                    his_common.dmc_nhom_mabhyt nhom,
                                his_common.org_organization khoa
		                    WHERE a.ma_lk = b.ma_lk
		                    AND b.ma_nhom = nhom.MANHOM_BHYT
                            AND b.MA_KHOA = khoa.MA_KHOA
		                    AND a.NGAY_RA BETWEEN @tungay AND @denngay
                            AND (b.MA_KHOA = @maKhoa OR @maKhoa='-1')
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
                p3.Value = req.MaKhoa.ToString();
                paramList.Add(p3);

                var data = await _context.dto_bc_doanhthu_khoa
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Khoa");

                // ====== 4 dòng đầu ======
                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Value = benhVien?.tenbenhvien;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A1").Style.Font.Bold = true;

                ws.Range("A2:I2").Merge();
                ws.Cell("A2").Value = "Phòng Tài chính - Kế toán";
                ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell("A2").Style.Font.Bold = true;

                ws.Range("A3:I3").Merge();
                ws.Cell("A3").Value = BuildTitle(req.TuNgay, req.DenNgay);
                ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A3").Style.Font.Bold = true;
                ws.Cell("A3").Style.Font.FontSize = 14;
                ws.Cell("A3").Style.Font.FontColor = XLColor.Blue;

                ws.Range("A4:I4").Merge();
                ws.Cell("A4").Value = $"Khoa: {(data.FirstOrDefault()?.khoa ?? "")}";
                ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A4").Style.Font.Bold = true;

                // Dòng 5 trống
                int row = 6;

                // ====== Header ======
                string[] headers =
                {
                    "STT",
                    "Khoa",
                    "Nội dung",
                    "Số lượt",
                    "Giá theo quy định",
                    "Chi phí vật tư, hóa chất",
                    "Hệ số",
                    "Điểm thực hiện",
                    "Ghi chú"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = headers[i];
                }

                var headerRange = ws.Range(row, 1, row, 9);
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

                decimal tongAllChiPhiVattu = 0;
                decimal tongAllDiem = 0;

                foreach (var group in groups)
                {
                    groupIndex++;

                    // Dòng tên nhóm: cột STT + merge 8 cột còn lại
                    ws.Cell(row, 1).Value = groupIndex;
                    ws.Range(row, 2, row, 9).Merge();
                    ws.Cell(row, 2).Value = group.Key ?? "";
                    ws.Range(row, 1, row, 9).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Range(row, 1, row, 9).Style.Alignment.WrapText = true;

                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                    ws.Range(row, 1, row, 9).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 9).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 9).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 9).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    row++;

                    int itemIndex = 0;

                    decimal tongChiPhiVattu = 0;
                    decimal tongDiem = 0;

                    foreach (var item in group)
                    {
                        itemIndex++;

                        ws.Cell(row, 1).Value = $"{groupIndex}.{itemIndex}";
                        ws.Cell(row, 2).Value = item.khoa ?? "";
                        ws.Cell(row, 3).Value = item.ten_dich_vu ?? "";
                        ws.Cell(row, 4).Value = item.soluong ?? 0;
                        ws.Cell(row, 5).Value = item.don_gia_bh ?? 0;
                        ws.Cell(row, 6).Value = item.chiphi_vattu ?? 0;
                        ws.Cell(row, 7).Value = item.heso ?? 0;
                        ws.Cell(row, 8).Value = item.diem_thuchien ?? 0;
                        ws.Cell(row, 9).Value = "";

                        // Căn lề
                        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                        // Định dạng số
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";

                        ws.Range(row, 1, row, 9).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 9).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 9).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 9).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        ws.Range(row, 1, row, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        tongChiPhiVattu += item.chiphi_vattu ?? 0;
                        tongDiem += item.diem_thuchien ?? 0;

                        tongAllChiPhiVattu += item.chiphi_vattu ?? 0;
                        tongAllDiem += item.diem_thuchien ?? 0;

                        ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        row++;
                    }
                    // ===== Dòng tổng =====
                    ws.Cell(row, 2).Value = "Tổng";

                    ws.Cell(row, 6).Value = tongChiPhiVattu;
                    ws.Cell(row, 8).Value = tongDiem;

                    // format
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";

                    ws.Range(row, 1, row, 9).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
                    ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }

                // ===== Dòng tổng cộng toàn bộ =====
                ws.Cell(row, 2).Value = "Tổng cộng";

                ws.Cell(row, 6).Value = tongAllChiPhiVattu;
                ws.Cell(row, 8).Value = tongAllDiem;

                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.##";

                ws.Range(row, 1, row, 9).Style.Font.Bold = true;
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(200, 200, 200);
                ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;

                // ====== Style chung ======
                ws.SheetView.FreezeRows(6);

                ws.Column(1).Width = 5;//
                ws.Column(2).Width = 20;//
                ws.Column(3).Width = 60;//
                ws.Column(4).Width = 10;//
                ws.Column(5).Width = 15;//
                ws.Column(6).Width = 15;//
                ws.Column(7).Width = 8;//
                ws.Column(8).Width = 15;
                ws.Column(9).Width = 10;//

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
            public string MaBacSy { get; set; }
        }

        public class BaoCaoKhoaRequest
        {
            public DateTime TuNgay { get; set; }
            public DateTime DenNgay { get; set; }
            public string? MaKhoa { get; set; }
        }

    }
}