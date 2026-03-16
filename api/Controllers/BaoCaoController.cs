using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Models;
using OfficeOpenXml;
using System.Globalization;
using OfficeOpenXml.Style;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "bao_cao")]
    public class BaoCaoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "bao_cao")]
        [HttpPost("xuat-bao-cao-nghi-phep-v1")]
        public async Task<IActionResult> XuatBaoCaoNghiPhepV1([FromBody] BaoCaoRequest request)
        {
            if (request.TuNgay > request.DenNgay)
            {
                return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
            }

            var danhSachNghiPhep = await _context.nghi_phep
                .Where(np => np.trang_thai == "3" &&
                             np.nghi_den >= request.TuNgay &&
                             np.nghi_tu <= request.DenNgay)
                .Join(_context.nhan_vien, np => np.ma_nv, nv => nv.ma_nv, (np, nv) => new { np, nv })
                .Where(n => n.nv.xoa != 1)
                .GroupJoin(_context.bo_phan, n => n.nv.bo_phan_id, bp => bp.id, (n, bpGroup) => new { n, bpGroup })
                .SelectMany(x => x.bpGroup.DefaultIfEmpty(), (x, bp) => new
                {
                    x.n.np.ma_nv,
                    x.n.nv.full_name,
                    ten_bo_phan = bp != null ? bp.ten_bo_phan : "N/A",
                    x.n.np.ngay_nghi,
                    x.n.np.ky_hieu_ly_do
                })
                .ToListAsync();

            int curYear = Math.Min(request.TuNgay.Year, request.DenNgay.Year);

            var danhSachPhepTon = await _context.phep_ton
                .Where(pt => pt.year == curYear.ToString())
                .ToDictionaryAsync(pt => pt.ma_nv, pt => pt.phep_ton);

            var danhSachNgay = Enumerable.Range(0, (request.DenNgay - request.TuNgay).Days + 1)
                                         .Select(i => request.TuNgay.AddDays(i))
                                         .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("BaoCaoNghiPhep");

            worksheet.Cells[5, 1].Value = "STT";
            worksheet.Cells[5, 2].Value = "Mã Nhân Viên";
            worksheet.Cells[5, 3].Value = "Họ Tên";
            worksheet.Cells[5, 4].Value = "Bộ Phận";
            for (int i = 0; i < danhSachNgay.Count; i++)
            {
                int columnIndex = i + 5;

                worksheet.Cells[5, columnIndex].Value = danhSachNgay[i].ToString("dd/MM");
                worksheet.Cells[6, columnIndex].Value = danhSachNgay[i].ToString("ddd", new CultureInfo("vi-VN")); // Hiển thị thứ bằng tiếng Việt

                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, 1].Merge = true;
            worksheet.Cells[5, 2, 6, 2].Merge = true;
            worksheet.Cells[5, 3, 6, 3].Merge = true;
            worksheet.Cells[5, 4, 6, 4].Merge = true;

            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            var headers = new List<string> { "A-Số ngày\nnghỉ việc\nkhông lương ",
                                             "H-Số ngày nghỉ\nphép năm",
                                             "Nghỉ chế độ không lương\nO-Bản thân ốm\nCO-Con ốm\nTS-Nghỉ khám thai/Nghỉ thai sản\nDS-Nghỉ dưỡng sức sau sinh",
                                             "S-Ngày nghỉ\nđặc biết có\nhưởng\nlương (hiếu/hỷ)",
                                             "Số ngày nghỉ không hưởng lương\nkhông bị trừ chuyên cần\nAP-Nghỉ không lương (ông/bà\nmất)",
                                             "K-Nghỉ kiểm kê/Nghỉ bù\nkiểm kê",
                                             "RH-Nghỉ\nphép về Nhật",
                                             "Nghỉ khác",
                                             "Tổng số ngày\nnghỉ",
                                             "Số ngày phép tồn còn lại tính đến\nhết tháng 12" };
            int startColumn = danhSachNgay.Count + 5;

            for (int i = 0; i < headers.Count; i++)
            {
                int columnIndex = startColumn + i;

                worksheet.Cells[5, columnIndex].Value = headers[i];

                worksheet.Cells[5, columnIndex, 6, columnIndex].Merge = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.WrapText = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.Font.Bold = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Row(5).Height = 210;


            var groupedData = danhSachNghiPhep.GroupBy(x => x.ma_nv).ToList();
            int row = 7, stt = 1;

            foreach (var group in groupedData)
            {
                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = group.Key;
                worksheet.Cells[row, 3].Value = group.First().full_name;
                worksheet.Cells[row, 4].Value = group.First().ten_bo_phan;

                int[] totals = new int[headers.Count - 1];

                var kyHieuTheoNgay = new Dictionary<int, string>();

                foreach (var item in group)
                {
                    var ngayNghiList = item.ngay_nghi.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(dateStr => DateTime.ParseExact(dateStr.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                                    .Where(date => date.Date >= request.TuNgay.Date && date.Date <= request.DenNgay.Date)
                                    .ToList();

                    foreach (var ngay in ngayNghiList)
                    {
                        int colIndex = danhSachNgay.FindIndex(d => d.Date == ngay.Date);
                        if (colIndex != -1)
                        {
                            kyHieuTheoNgay[colIndex] = item.ky_hieu_ly_do;
                            worksheet.Cells[row, colIndex + 5].Value = item.ky_hieu_ly_do;
                        }
                    }
                }

                foreach (var kyHieu in kyHieuTheoNgay.Values)
                {
                    switch (kyHieu)
                    {
                        case "A": totals[0]++; break;
                        case "H": totals[1]++; break;
                        case "O":
                        case "CO":
                        case "TS":
                        case "DS": totals[2]++; break;
                        case "S": totals[3]++; break;
                        case "AP": totals[4]++; break;
                        case "K": totals[5]++; break;
                        case "RH": totals[6]++; break;
                        case "#": totals[7]++; break;
                    }
                }

                totals[8] = totals.Sum();

                for (int i = 0; i < totals.Length; i++)
                {
                    worksheet.Cells[row, danhSachNgay.Count + 5 + i].Value = totals[i];
                }

                worksheet.Cells[row, danhSachNgay.Count + 5 + totals.Length].Value = danhSachPhepTon.ContainsKey(group.Key)
                    ? danhSachPhepTon[group.Key]
                    : 0;

                row++;
            }
            var totalColumns = danhSachNgay.Count + 5 + headers.Count - 1;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells.AutoFitColumns();

            worksheet.Cells[1, 1].Value = "Công Ty TNHH Sinfonia Microtec Việt Nam ";
            worksheet.Cells[2, 1].Value = "KCN Đồng Văn II, Duy Tiên, Duy Minh, Hà Nam.";

            worksheet.Cells[4, 1].Value = $"BÁO CÁO NGÀY NGHỈ CHI TIẾT TỪ NGÀY {request.TuNgay:dd/MM/yyyy} ĐẾN NGÀY {request.DenNgay:dd/MM/yyyy}";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Size = 14;
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"BaoCaoNghiPhep_from_{request.TuNgay:ddMMyyyy}_to_{request.DenNgay:ddMMyyyy}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        [Authorize(Roles = "bao_cao")]
        [HttpPost("xuat-bao-cao-nghi-phep")]
        public async Task<IActionResult> XuatBaoCaoNghiPhep([FromBody] BaoCaoRequest request)
        {
            if (request.TuNgay > request.DenNgay)
            {
                return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
            }
            var danhSachNghiPhep = await (
                                    from nv in _context.nhan_vien
                                    where nv.xoa != 1
                                    join np in _context.nghi_phep.Where(np => np.trang_thai == "3" &&
                                                                                np.nghi_den >= request.TuNgay &&
                                                                                np.nghi_tu <= request.DenNgay)
                                        on nv.ma_nv equals np.ma_nv into nghiPhepGroup
                                    from np in nghiPhepGroup.DefaultIfEmpty()
                                    join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                                    from bp in bpGroup.DefaultIfEmpty()
                                    select new
                                    {
                                        ma_nv = nv.ma_nv,
                                        full_name = nv.full_name,
                                        ten_bo_phan = bp != null ? bp.ten_bo_phan : "N/A",
                                        // Nếu không có dữ liệu nghỉ phép thì các trường này sẽ là null
                                        ngay_nghi = np != null ? np.ngay_nghi : null,
                                        ky_hieu_ly_do = np != null ? np.ky_hieu_ly_do : null
                                    }
                                ).ToListAsync();


            int curYear = Math.Min(request.TuNgay.Year, request.DenNgay.Year);

            var danhSachPhepTon = await _context.phep_ton
                .Where(pt => pt.year == curYear.ToString())
                .ToDictionaryAsync(pt => pt.ma_nv, pt => pt.phep_ton);

            var danhSachNgay = Enumerable.Range(0, (request.DenNgay - request.TuNgay).Days + 1)
                                         .Select(i => request.TuNgay.AddDays(i))
                                         .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("BaoCaoNghiPhep");

            // Header thông tin chung
            worksheet.Cells[5, 1].Value = "STT";
            worksheet.Cells[5, 2].Value = "Mã Nhân Viên";
            worksheet.Cells[5, 3].Value = "Họ Tên";
            worksheet.Cells[5, 4].Value = "Bộ Phận";
            for (int i = 0; i < danhSachNgay.Count; i++)
            {
                int columnIndex = i + 5;
                worksheet.Cells[5, columnIndex].Value = danhSachNgay[i].ToString("dd/MM");
                worksheet.Cells[6, columnIndex].Value = danhSachNgay[i].ToString("ddd", new CultureInfo("vi-VN")); // Hiển thị thứ bằng tiếng Việt
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, 1].Merge = true;
            worksheet.Cells[5, 2, 6, 2].Merge = true;
            worksheet.Cells[5, 3, 6, 3].Merge = true;
            worksheet.Cells[5, 4, 6, 4].Merge = true;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Các header của cột tổng hợp (chỉ tính các loại nghỉ)
            var headers = new List<string> {
        "A-Số ngày\nnghỉ việc\nkhông lương ",
        "H-Số ngày nghỉ\nphép năm",
        "Nghỉ chế độ không lương\nO-Bản thân ốm\nCO-Con ốm\nTS-Nghỉ khám thai/Nghỉ thai sản\nDS-Nghỉ dưỡng sức sau sinh",
        "S-Ngày nghỉ\nđặc biết có\nhưởng\nlương (hiếu/hỷ)",
        "Số ngày nghỉ không hưởng lương\nkhông bị trừ chuyên cần\nAP-Nghỉ không lương (ông/bà\nmất)",
        "K-Nghỉ kiểm kê/Nghỉ bù\nkiểm kê",
        "RH-Nghỉ\nphép về Nhật",
        "Nghỉ khác",
        "Tổng số ngày\nnghỉ",
        "Số ngày phép tồn còn lại tính đến\nhết tháng 12"
    };
            int startColumn = danhSachNgay.Count + 5;

            for (int i = 0; i < headers.Count; i++)
            {
                int columnIndex = startColumn + i;
                worksheet.Cells[5, columnIndex].Value = headers[i];
                worksheet.Cells[5, columnIndex, 6, columnIndex].Merge = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.WrapText = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.Font.Bold = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(5).Height = 210;

            // Xử lý dữ liệu chi tiết từng nhân viên và đồng thời tích lũy tổng theo bộ phận
            var groupedData = danhSachNghiPhep.GroupBy(x => x.ma_nv).ToList();
            int row = 7, stt = 1;
            // Dictionary lưu tổng hợp theo bộ phận (key: ten_bo_phan, value: mảng tổng theo 9 loại)
            var departmentTotals = new Dictionary<string, int[]>();

            foreach (var group in groupedData)
            {
                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = group.Key;
                worksheet.Cells[row, 3].Value = group.First().full_name;
                worksheet.Cells[row, 4].Value = group.First().ten_bo_phan;

                int[] totals = new int[headers.Count - 1]; // 9 phần tử: 0-7 là các loại, 8 là tổng số ngày nghỉ
                var kyHieuTheoNgay = new Dictionary<int, string>();

                foreach (var item in group)
                {
                    if (item.ngay_nghi == null) continue;
                    var ngayNghiList = item.ngay_nghi
                        .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(dateStr => DateTime.ParseExact(dateStr.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                        .Where(date => date.Date >= request.TuNgay.Date && date.Date <= request.DenNgay.Date)
                        .ToList();

                    foreach (var ngay in ngayNghiList)
                    {
                        int colIndex = danhSachNgay.FindIndex(d => d.Date == ngay.Date);
                        if (colIndex != -1)
                        {
                            // Ghi thông tin ký hiệu nghỉ vào ô ứng với ngày tương ứng
                            kyHieuTheoNgay[colIndex] = item.ky_hieu_ly_do;
                            worksheet.Cells[row, colIndex + 5].Value = item.ky_hieu_ly_do;
                        }
                    }
                }

                // Tính tổng các loại nghỉ theo từng ký hiệu (chỉ đếm 1 lần cho mỗi ngày của 1 nhân viên)
                foreach (var kyHieu in kyHieuTheoNgay.Values)
                {
                    switch (kyHieu)
                    {
                        case "A": totals[0]++; break;
                        case "H": totals[1]++; break;
                        case "O":
                        case "CO":
                        case "TS":
                        case "DS": totals[2]++; break;
                        case "S": totals[3]++; break;
                        case "AP": totals[4]++; break;
                        case "K": totals[5]++; break;
                        case "RH": totals[6]++; break;
                        case "#": totals[7]++; break;
                    }
                }
                totals[8] = totals.Take(8).Sum();

                // Ghi thông tin tổng của nhân viên vào các cột sau phần ngày
                for (int i = 0; i < totals.Length; i++)
                {
                    worksheet.Cells[row, danhSachNgay.Count + 5 + i].Value = totals[i];
                }
                worksheet.Cells[row, danhSachNgay.Count + 5 + totals.Length].Value = danhSachPhepTon.ContainsKey(group.Key)
                    ? danhSachPhepTon[group.Key]
                    : 0;

                // Cộng dồn vào tổng của bộ phận
                string dept = group.First().ten_bo_phan;
                if (!departmentTotals.ContainsKey(dept))
                    departmentTotals[dept] = new int[9];
                for (int i = 0; i < 9; i++)
                {
                    departmentTotals[dept][i] += totals[i];
                }
                row++;
            }

            // Vẽ border cho các ô dữ liệu chi tiết
            var totalColumns = danhSachNgay.Count + 5 + headers.Count - 1;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // -----------------------------------------------
            // Phần tổng hợp theo bộ phận
            // Thêm tiêu đề cho phần tổng hợp
            worksheet.Cells[row, 1].Value = "TỔNG HỢP THEO BỘ PHẬN";
            worksheet.Cells[row, 1, row, danhSachNgay.Count + 14].Merge = true;
            worksheet.Cells[row, 1, row, danhSachNgay.Count + 14].Style.Font.Bold = true;

            row++;

            int sttDept = 1;
            foreach (var kvp in departmentTotals)
            {
                string deptName = kvp.Key;
                int[] deptTotals = kvp.Value;
                worksheet.Cells[row, 1].Value = sttDept++;

                worksheet.Cells[row, 2, row, danhSachNgay.Count + 4].Merge = true;

                worksheet.Cells[row, 2].Value = deptName;
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                // Các cột ngày nghỉ (từ cột 5 đến cột danhSachNgay.Count+4) để trống
                // Ghi tổng hợp các loại nghỉ từ cột tổng (bắt đầu từ cột danhSachNgay.Count+5)
                for (int i = 0; i < deptTotals.Length; i++)
                {
                    worksheet.Cells[row, danhSachNgay.Count + 5 + i].Value = deptTotals[i];
                }
                // Cột "Số ngày phép tồn" để trống hoặc hiển thị dấu gạch ngang
                worksheet.Cells[row, danhSachNgay.Count + 5 + deptTotals.Length].Value = "-";
                row++;
            }

            // Vẽ border cho phần tổng hợp (bao gồm cả phần chi tiết và tổng hợp)
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells.AutoFitColumns();

            // Phần tiêu đề báo cáo
            worksheet.Cells[1, 1].Value = "Công Ty TNHH Sinfonia Microtec Việt Nam ";
            worksheet.Cells[2, 1].Value = "KCN Đồng Văn II, Duy Tiên, Duy Minh, Hà Nam.";
            worksheet.Cells[4, 1].Value = $"BÁO CÁO NGÀY NGHỈ CHI TIẾT TỪ NGÀY {request.TuNgay:dd/MM/yyyy} ĐẾN NGÀY {request.DenNgay:dd/MM/yyyy}";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Size = 14;

            foreach (var cell in worksheet.Cells)
            {
                if (cell.Value != null && cell.Value is string cellText)
                {
                    // Thay thế tất cả ký tự '#' bằng 'NP'
                    cell.Value = cellText.Replace("#", "NK");
                }
            }
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"BaoCaoNghiPhep_from_{request.TuNgay:ddMMyyyy}_to_{request.DenNgay:ddMMyyyy}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        [Authorize(Roles = "bao_bp_cao_bo_phan")]
        [HttpPost("xuat-bao-cao-nghi-phep-bo-phan")]
        public async Task<IActionResult> XuatBaoCaoNghiPhepBoPhan([FromBody] BaoCaoRequest request)
        {
            if (request.TuNgay > request.DenNgay)
            {
                return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
            }
            var bo_phan = User.FindFirst("ten_bo_phan").Value;
            // Left join từ nhân viên với nghi_phep để đảm bảo luôn có tất cả nhân viên, dù không có ngày nghỉ
            var danhSachNghiPhep = await (
                                           from nv in _context.nhan_vien
                                           where nv.xoa != 1
                                           join np in _context.nghi_phep.Where(np => np.trang_thai == "3" &&
                                                                                       np.nghi_den >= request.TuNgay &&
                                                                                       np.nghi_tu <= request.DenNgay)
                                               on nv.ma_nv equals np.ma_nv into nghiPhepGroup
                                           from np in nghiPhepGroup.DefaultIfEmpty()
                                           join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                                           from bp in bpGroup.DefaultIfEmpty()
                                           select new
                                           {
                                               ma_nv = nv.ma_nv,
                                               full_name = nv.full_name,
                                               ten_bo_phan = bp != null ? bp.ten_bo_phan : "N/A",
                                               // Nếu không có dữ liệu nghỉ phép thì các trường này sẽ là null
                                               ngay_nghi = np != null ? np.ngay_nghi : null,
                                               ky_hieu_ly_do = np != null ? np.ky_hieu_ly_do : null
                                           }
                                       )
                       .Where(x => x.ten_bo_phan == bo_phan)
                       .ToListAsync();

            int curYear = Math.Min(request.TuNgay.Year, request.DenNgay.Year);

            var danhSachPhepTon = await _context.phep_ton
                .Where(pt => pt.year == curYear.ToString())
                .ToDictionaryAsync(pt => pt.ma_nv, pt => pt.phep_ton);

            var danhSachNgay = Enumerable.Range(0, (request.DenNgay - request.TuNgay).Days + 1)
                                         .Select(i => request.TuNgay.AddDays(i))
                                         .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("BaoCaoNghiPhep");

            // Header thông tin chung
            worksheet.Cells[5, 1].Value = "STT";
            worksheet.Cells[5, 2].Value = "Mã Nhân Viên";
            worksheet.Cells[5, 3].Value = "Họ Tên";
            worksheet.Cells[5, 4].Value = "Bộ Phận";
            for (int i = 0; i < danhSachNgay.Count; i++)
            {
                int columnIndex = i + 5;
                worksheet.Cells[5, columnIndex].Value = danhSachNgay[i].ToString("dd/MM");
                worksheet.Cells[6, columnIndex].Value = danhSachNgay[i].ToString("ddd", new CultureInfo("vi-VN")); // Hiển thị thứ bằng tiếng Việt
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, 1].Merge = true;
            worksheet.Cells[5, 2, 6, 2].Merge = true;
            worksheet.Cells[5, 3, 6, 3].Merge = true;
            worksheet.Cells[5, 4, 6, 4].Merge = true;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, danhSachNgay.Count + 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Các header của cột tổng hợp (chỉ tính các loại nghỉ)
            var headers = new List<string> {
                "A-Số ngày\nnghỉ việc\nkhông lương ",
                "H-Số ngày nghỉ\nphép năm",
                "Nghỉ chế độ không lương\nO-Bản thân ốm\nCO-Con ốm\nTS-Nghỉ khám thai/Nghỉ thai sản\nDS-Nghỉ dưỡng sức sau sinh",
                "S-Ngày nghỉ\nđặc biết có\nhưởng\nlương (hiếu/hỷ)",
                "Số ngày nghỉ không hưởng lương\nkhông bị trừ chuyên cần\nAP-Nghỉ không lương (ông/bà\nmất)",
                "K-Nghỉ kiểm kê/Nghỉ bù\nkiểm kê",
                "RH-Nghỉ\nphép về Nhật",
                "Nghỉ khác",
                "Tổng số ngày\nnghỉ",
                "Số ngày phép tồn còn lại tính đến\nhết tháng 12"
            };
            int startColumn = danhSachNgay.Count + 5;

            for (int i = 0; i < headers.Count; i++)
            {
                int columnIndex = startColumn + i;
                worksheet.Cells[5, columnIndex].Value = headers[i];
                worksheet.Cells[5, columnIndex, 6, columnIndex].Merge = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.WrapText = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.Font.Bold = true;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[5, columnIndex, 6, columnIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.Font.Bold = true;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[5, 1, 6, startColumn + headers.Count - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(5).Height = 210;

            // Xử lý dữ liệu chi tiết từng nhân viên và đồng thời tích lũy tổng theo bộ phận
            var groupedData = danhSachNghiPhep.GroupBy(x => x.ma_nv).ToList();
            int row = 7, stt = 1;
            // Dictionary lưu tổng hợp theo bộ phận (key: ten_bo_phan, value: mảng tổng theo 9 loại)
            var departmentTotals = new Dictionary<string, int[]>();

            foreach (var group in groupedData)
            {
                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = group.Key;
                worksheet.Cells[row, 3].Value = group.First().full_name;
                worksheet.Cells[row, 4].Value = group.First().ten_bo_phan;

                int[] totals = new int[headers.Count - 1];
                var kyHieuTheoNgay = new Dictionary<int, string>();

                foreach (var item in group)
                {
                    if (item.ngay_nghi == null) continue;
                    var ngayNghiList = item.ngay_nghi
                        .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(dateStr => DateTime.ParseExact(dateStr.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                        .Where(date => date.Date >= request.TuNgay.Date && date.Date <= request.DenNgay.Date)
                        .ToList();

                    foreach (var ngay in ngayNghiList)
                    {
                        int colIndex = danhSachNgay.FindIndex(d => d.Date == ngay.Date);
                        if (colIndex != -1)
                        {
                            kyHieuTheoNgay[colIndex] = item.ky_hieu_ly_do;
                            worksheet.Cells[row, colIndex + 5].Value = item.ky_hieu_ly_do;
                        }
                    }
                }

                foreach (var kyHieu in kyHieuTheoNgay.Values)
                {
                    switch (kyHieu)
                    {
                        case "A": totals[0]++; break;
                        case "H": totals[1]++; break;
                        case "O":
                        case "CO":
                        case "TS":
                        case "DS": totals[2]++; break;
                        case "S": totals[3]++; break;
                        case "AP": totals[4]++; break;
                        case "K": totals[5]++; break;
                        case "RH": totals[6]++; break;
                        case "#": totals[7]++; break;
                    }
                }
                totals[8] = totals.Take(8).Sum();

                for (int i = 0; i < totals.Length; i++)
                {
                    worksheet.Cells[row, danhSachNgay.Count + 5 + i].Value = totals[i];
                }
                worksheet.Cells[row, danhSachNgay.Count + 5 + totals.Length].Value = danhSachPhepTon.ContainsKey(group.Key)
                    ? danhSachPhepTon[group.Key]
                    : 0;

                string dept = group.First().ten_bo_phan;
                if (!departmentTotals.ContainsKey(dept))
                    departmentTotals[dept] = new int[9];
                for (int i = 0; i < 9; i++)
                {
                    departmentTotals[dept][i] += totals[i];
                }
                row++;
            }

            var totalColumns = danhSachNgay.Count + 5 + headers.Count - 1;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            worksheet.Cells[row, 1].Value = "TỔNG HỢP THEO BỘ PHẬN";
            worksheet.Cells[row, 1, row, danhSachNgay.Count + 14].Merge = true;
            worksheet.Cells[row, 1, row, danhSachNgay.Count + 14].Style.Font.Bold = true;

            row++;

            int sttDept = 1;
            foreach (var kvp in departmentTotals)
            {
                string deptName = kvp.Key;
                int[] deptTotals = kvp.Value;
                worksheet.Cells[row, 1].Value = sttDept++;

                worksheet.Cells[row, 2, row, danhSachNgay.Count + 4].Merge = true;

                worksheet.Cells[row, 2].Value = deptName;
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                for (int i = 0; i < deptTotals.Length; i++)
                {
                    worksheet.Cells[row, danhSachNgay.Count + 5 + i].Value = deptTotals[i];
                }
                worksheet.Cells[row, danhSachNgay.Count + 5 + deptTotals.Length].Value = "-";
                row++;
            }

            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[5, 1, row - 1, totalColumns].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells.AutoFitColumns();

            worksheet.Cells[1, 1].Value = "Công Ty TNHH Sinfonia Microtec Việt Nam ";
            worksheet.Cells[2, 1].Value = "KCN Đồng Văn II, Duy Tiên, Duy Minh, Hà Nam.";
            worksheet.Cells[4, 1].Value = $"BÁO CÁO NGÀY NGHỈ CHI TIẾT TỪ NGÀY {request.TuNgay:dd/MM/yyyy} ĐẾN NGÀY {request.DenNgay:dd/MM/yyyy}";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Size = 14;

            foreach (var cell in worksheet.Cells)
            {
                if (cell.Value != null && cell.Value is string cellText)
                {
                    cell.Value = cellText.Replace("#", "NK");
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"BaoCaoNghiPhep_from_{request.TuNgay:ddMMyyyy}_to_{request.DenNgay:ddMMyyyy}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        [Authorize(Roles = "bao_bp_cao_bo_phan")]
        [HttpPost("XuatBaoCaoNghiPhepBoPhanJson")]
        public async Task<IActionResult> XuatBaoCaoNghiPhepBoPhanJson([FromBody] BaoCaoRequest_BoPhan request)
        {
            if (request.TuNgay > request.DenNgay)
            {
                return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
            }

            // Lấy tên bộ phận từ claim của user
            var bo_phan = User.FindFirst("ten_bo_phan")?.Value;

            // Xây dựng truy vấn cơ bản từ bảng nghỉ phép và join với nhân viên
            var query = _context.nghi_phep
                .Where(np => np.trang_thai == "3" &&
                             np.nghi_den >= request.TuNgay &&
                             np.nghi_tu <= request.DenNgay)
                .Join(_context.nhan_vien, np => np.ma_nv, nv => nv.ma_nv, (np, nv) => new { np, nv })
                .Where(n => n.nv.xoa != 1);

            // Nếu searchTerm khác "All" thì thêm điều kiện tìm kiếm theo nv.full_name hoặc nv.ma_nv
            if (!string.IsNullOrEmpty(request.searchTerm) && request.searchTerm != "All")
            {
                string lowerSearchTerm = request.searchTerm.ToLower();
                query = query.Where(x => x.nv.full_name.ToLower().Contains(lowerSearchTerm) ||
                                         x.nv.ma_nv.ToLower().Contains(lowerSearchTerm));
            }

            // Tiếp tục join với bảng bộ phận
            var danhSachNghiPhep = await query
                .GroupJoin(_context.bo_phan, n => n.nv.bo_phan_id, bp => bp.id, (n, bpGroup) => new { n, bpGroup })
                .SelectMany(x => x.bpGroup.DefaultIfEmpty(), (x, bp) => new
                {
                    x.n.np.ma_nv,
                    x.n.nv.full_name,
                    ten_bo_phan = bp != null ? bp.ten_bo_phan : "N/A",
                    x.n.np.ngay_nghi,
                    x.n.np.ky_hieu_ly_do
                })
                .Where(x => x.ten_bo_phan == bo_phan)
                .ToListAsync();

            // Lấy danh sách số ngày phép tồn theo nhân viên
            int curYear = Math.Min(request.TuNgay.Year, request.DenNgay.Year);

            var danhSachPhepTon = await _context.phep_ton
                .Where(pt => pt.year == curYear.ToString())
                .ToDictionaryAsync(pt => pt.ma_nv, pt => pt.phep_ton);

            // Nhóm dữ liệu theo mã nhân viên
            var groupedData = danhSachNghiPhep.GroupBy(x => x.ma_nv).ToList();
            int stt = 1;
            var resultList = new List<object>();

            // Tính tổng số ngày nghỉ theo từng loại cho mỗi nhân viên (mỗi ngày chỉ tính 1 lần)
            foreach (var group in groupedData)
            {
                var leaveByDate = new Dictionary<DateTime, string>();
                foreach (var item in group)
                {
                    if (string.IsNullOrWhiteSpace(item.ngay_nghi))
                        continue;
                    var ngayList = item.ngay_nghi
                        .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(dateStr => DateTime.ParseExact(dateStr.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                        .Where(date => date.Date >= request.TuNgay.Date && date.Date <= request.DenNgay.Date);
                    foreach (var ngay in ngayList)
                    {
                        // Nếu có nhiều bản ghi cùng ngày thì giá trị cuối cùng sẽ được ghi đè
                        leaveByDate[ngay.Date] = item.ky_hieu_ly_do;
                    }
                }

                // Mảng totals gồm 9 phần tử: 
                // 0: "A", 1: "H", 2: (O, CO, TS, DS), 3: "S", 4: "AP", 5: "K", 6: "RH", 7: "#", 8: Tổng
                int[] totals = new int[9];
                foreach (var kyHieu in leaveByDate.Values)
                {
                    switch (kyHieu)
                    {
                        case "A": totals[0]++; break;
                        case "H": totals[1]++; break;
                        case "O":
                        case "CO":
                        case "TS":
                        case "DS": totals[2]++; break;
                        case "S": totals[3]++; break;
                        case "AP": totals[4]++; break;
                        case "K": totals[5]++; break;
                        case "RH": totals[6]++; break;
                        case "#": totals[7]++; break;
                    }
                }
                totals[8] = totals.Take(8).Sum();

                // Tạo đối tượng kết quả cho từng nhân viên
                var employeeResult = new
                {
                    stt = stt++,
                    ma_nv = group.Key,
                    full_name = group.First().full_name,
                    ten_bo_phan = group.First().ten_bo_phan,
                    A = totals[0],
                    H = totals[1],
                    O_CO_TS_DS = totals[2],
                    S = totals[3],
                    AP = totals[4],
                    K = totals[5],
                    RH = totals[6],
                    NK = totals[7], // Thay "#" bằng "NK" khi hiển thị
                    total = totals[8],
                    phep_ton = danhSachPhepTon.ContainsKey(group.Key) ? danhSachPhepTon[group.Key] : 0
                };

                resultList.Add(employeeResult);
            }

            // Phân trang dữ liệu
            int totalCount = resultList.Count;
            var pagedItems = resultList
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Items = pagedItems
            });
        }
        [Authorize(Roles = "bao_cao")]
        [HttpPost("XuatBaoCaoNghiPhepJson")]
        public async Task<IActionResult> XuatBaoCaoNghiPhepJson([FromBody] BaoCaoRequest_BoPhan request)
        {
            if (request.TuNgay > request.DenNgay)
            {
                return BadRequest("Ngày bắt đầu không thể lớn hơn ngày kết thúc.");
            }

            var query = _context.nghi_phep
                .Where(np => np.trang_thai == "3" &&
                             np.nghi_den >= request.TuNgay &&
                             np.nghi_tu <= request.DenNgay)
                .Join(_context.nhan_vien, np => np.ma_nv, nv => nv.ma_nv, (np, nv) => new { np, nv })
                .Where(n => n.nv.xoa != 1);

            var danhSachNghiPhep = await query
                .GroupJoin(_context.bo_phan, n => n.nv.bo_phan_id, bp => bp.id, (n, bpGroup) => new { n, bpGroup })
                .SelectMany(x => x.bpGroup.DefaultIfEmpty(), (x, bp) => new
                {
                    x.n.np.ma_nv,
                    x.n.nv.full_name,
                    ten_bo_phan = bp != null ? bp.ten_bo_phan : "N/A",
                    x.n.np.ngay_nghi,
                    x.n.np.ky_hieu_ly_do
                })
            .ToListAsync();

            // Nếu searchTerm khác "All" thì thêm điều kiện tìm kiếm theo nv.full_name hoặc nv.ma_nv
            if (!string.IsNullOrEmpty(request.searchTerm) && request.searchTerm != "All")
            {
                string lowerSearchTerm = request.searchTerm.ToLower();
                danhSachNghiPhep = danhSachNghiPhep.Where(x => x.full_name.ToLower().Contains(lowerSearchTerm) ||
                                                            x.ma_nv.ToLower().Contains(lowerSearchTerm) ||
                                                            x.ten_bo_phan.ToLower().Contains(lowerSearchTerm)).ToList();
            }

            int curYear = Math.Min(request.TuNgay.Year, request.DenNgay.Year);

            var danhSachPhepTon = await _context.phep_ton
                .Where(pt => pt.year == curYear.ToString())
                .ToDictionaryAsync(pt => pt.ma_nv, pt => pt.phep_ton);

            var groupedData = danhSachNghiPhep.GroupBy(x => x.ma_nv).ToList();
            int stt = 1;
            var resultList = new List<object>();

            foreach (var group in groupedData)
            {
                var leaveByDate = new Dictionary<DateTime, string>();
                foreach (var item in group)
                {
                    if (string.IsNullOrWhiteSpace(item.ngay_nghi))
                        continue;
                    var ngayList = item.ngay_nghi
                        .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(dateStr => DateTime.ParseExact(dateStr.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                        .Where(date => date.Date >= request.TuNgay.Date && date.Date <= request.DenNgay.Date);
                    foreach (var ngay in ngayList)
                    {
                        leaveByDate[ngay.Date] = item.ky_hieu_ly_do;
                    }
                }

                // Mảng totals gồm 9 phần tử: 
                // 0: "A", 1: "H", 2: (O, CO, TS, DS), 3: "S", 4: "AP", 5: "K", 6: "RH", 7: "#", 8: Tổng
                int[] totals = new int[9];
                foreach (var kyHieu in leaveByDate.Values)
                {
                    switch (kyHieu)
                    {
                        case "A": totals[0]++; break;
                        case "H": totals[1]++; break;
                        case "O":
                        case "CO":
                        case "TS":
                        case "DS": totals[2]++; break;
                        case "S": totals[3]++; break;
                        case "AP": totals[4]++; break;
                        case "K": totals[5]++; break;
                        case "RH": totals[6]++; break;
                        case "#": totals[7]++; break;
                    }
                }
                totals[8] = totals.Take(8).Sum();

                // Tạo đối tượng kết quả cho từng nhân viên
                var employeeResult = new
                {
                    stt = stt++,
                    ma_nv = group.Key,
                    full_name = group.First().full_name,
                    ten_bo_phan = group.First().ten_bo_phan,
                    A = totals[0],
                    H = totals[1],
                    O_CO_TS_DS = totals[2],
                    S = totals[3],
                    AP = totals[4],
                    K = totals[5],
                    RH = totals[6],
                    NK = totals[7],
                    total = totals[8],
                    phep_ton = danhSachPhepTon.ContainsKey(group.Key) ? danhSachPhepTon[group.Key] : 0
                };

                resultList.Add(employeeResult);
            }

            int totalCount = resultList.Count;
            var pagedItems = resultList
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Items = pagedItems
            });
        }
    }
    public class BaoCaoRequest
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
    }
    public class BaoCaoRequest_BoPhan
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string searchTerm { get; set; }
    }
}