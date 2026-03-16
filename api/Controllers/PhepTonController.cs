using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Models;
using OfficeOpenXml;
using System.Globalization;

[Route("api/phep-ton")]
[ApiController]
public class PhepTonController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PhepTonController(ApplicationDbContext context)
    {
        _context = context;
    }
    [Authorize(Roles = "admin")]
    [HttpPost("import-phep-ton")]
    public async Task<IActionResult> ImportPhepTon([FromQuery] string year, IFormFile file)
    {
        if (await _context.phep_ton.AnyAsync(p => p.year == year))
        {
            // return BadRequest(new { code = 1, message = "Dữ liệu cho năm này đã tồn tại." });
            return await UpdatePhepTon(year, file);
        }
        if (!int.TryParse(year, out int numericYear) || numericYear < 2024 || numericYear > 2030)
        {
            return BadRequest(new { code = 2, message = "Năm nhập không hợp lệ. Vui lòng nhập năm từ 2024 đến 2030." });
        }

        // Lấy danh sách nhân viên có xoa == 0
        var nhanViens = await _context.nhan_vien
            .Where(nv => nv.xoa == 0)
            .ToDictionaryAsync(nv => nv.ma_nv, nv => nv);

        var phepTonList = new List<PhepTon>();

        // Danh sách lỗi chuyển đổi phép tồn (conversion error)
        var conversionErrorList = new List<(string ID, string InvalidValue)>();

        // Danh sách các ID có trong file nhưng không tồn tại trong bảng nhan_vien
        var notFoundInNhanVienList = new List<(string ID, int TotalRemain)>();

        // Danh sách nhân viên không có trong file (sẽ được set phép tồn = 0)
        var notFoundList = new List<string>();

        var updatedCount = 0;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    string id = worksheet.Cells[row, 2].Text.Trim();
                    string totalRemainText = worksheet.Cells[row, 9].Text.Trim();

                    // Nếu không thể chuyển đổi sang số, thêm vào danh sách lỗi chuyển đổi
                    if (!int.TryParse(totalRemainText, NumberStyles.Any, CultureInfo.InvariantCulture, out int totalRemain))
                    {
                        conversionErrorList.Add((id, totalRemainText));
                        continue;
                    }

                    // Nếu ID tồn tại trong bảng nhân viên, thêm vào phepTonList và tăng updatedCount
                    if (nhanViens.ContainsKey(id))
                    {
                        phepTonList.Add(new PhepTon { ma_nv = id, year = year, phep_ton = totalRemain });
                        updatedCount++;
                    }
                    else
                    {
                        // Trường hợp có ID nhưng không tồn tại trong bảng nhân viên
                        notFoundInNhanVienList.Add((id, totalRemain));
                    }
                }
            }
        }

        // Với các nhân viên có trong bảng nhan_vien nhưng không có trong file Excel,
        // thêm vào phepTonList với phep_ton = 0 và lưu lại vào notFoundList
        foreach (var ma_nv in nhanViens.Keys)
        {
            if (!phepTonList.Any(p => p.ma_nv == ma_nv))
            {
                phepTonList.Add(new PhepTon { ma_nv = ma_nv, year = year, phep_ton = 0 });
                notFoundList.Add(ma_nv);
            }
        }

        await _context.phep_ton.AddRangeAsync(phepTonList);
        await _context.SaveChangesAsync();

        // Tạo file Excel báo cáo kết quả import
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Kết quả cập nhật");
            worksheet.Cells[1, 1].Value = "Số lượng cập nhật thành công:";
            worksheet.Cells[1, 2].Value = updatedCount;
            worksheet.Cells[2, 1].Value = "Tổng số nhân viên trong file:";
            // Tổng số nhân viên trong file = số nhân viên xử lý thành công + lỗi chuyển đổi + ID không có trong bảng nhân viên
            worksheet.Cells[2, 2].Value = updatedCount + conversionErrorList.Count + notFoundInNhanVienList.Count;

            int currentRow = 4;

            // Hiển thị danh sách lỗi chuyển đổi
            if (conversionErrorList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên lỗi chuyển đổi phép tồn sang dạng số";
                worksheet.Cells[currentRow, 2].Value = "Giá trị không hợp lệ";
                currentRow++;
                foreach (var (ID, invalidValue) in conversionErrorList)
                {
                    worksheet.Cells[currentRow, 1].Value = ID;
                    worksheet.Cells[currentRow, 2].Value = invalidValue;
                    currentRow++;
                }
                currentRow++; // dòng trống
            }

            // Hiển thị danh sách các nhân viên có ID không tồn tại trong bảng nhân viên
            if (notFoundInNhanVienList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên có ID không tồn tại trong bảng nhân viên";
                worksheet.Cells[currentRow, 2].Value = "Total Remain";
                currentRow++;
                foreach (var (ID, totalRemain) in notFoundInNhanVienList)
                {
                    worksheet.Cells[currentRow, 1].Value = ID;
                    worksheet.Cells[currentRow, 2].Value = totalRemain;
                    currentRow++;
                }
                currentRow++; // dòng trống
            }

            // Hiển thị danh sách các nhân viên không có trong file (được set phép tồn = 0)
            if (notFoundList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên không có trong file + lỗi chuyển đổi (phép tồn đặt thành 0)";
                currentRow++;
                foreach (var ma_nv in notFoundList)
                {
                    worksheet.Cells[currentRow, 1].Value = ma_nv;
                    worksheet.Cells[currentRow, 2].Value = 0;
                    currentRow++;
                }
            }
            worksheet.Cells.AutoFitColumns();

            var resultStream = new MemoryStream();
            package.SaveAs(resultStream);
            resultStream.Position = 0;
            return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KetQuaImportPhepTonExcel.xlsx");
        }
    }
    // [Authorize(Roles = "admin")]
    // [HttpPost("update-phep-ton")]
    private async Task<IActionResult> UpdatePhepTon([FromQuery] string year, IFormFile file)
    {
        // Kiểm tra xem đã có dữ liệu phép tồn của năm này hay chưa (nếu chưa có thì không thể cập nhật)
        if (!await _context.phep_ton.AnyAsync(p => p.year == year))
        {
            return BadRequest(new { code = 1, message = "Không có dữ liệu cho năm này để cập nhật." });
        }

        if (!int.TryParse(year, out int numericYear) || numericYear < 2024 || numericYear > 2030)
        {
            return BadRequest(new { code = 2, message = "Năm nhập không hợp lệ. Vui lòng nhập năm từ 2024 đến 2030." });
        }

        // Lấy danh sách nhân viên (chỉ những nhân viên chưa xóa)
        var nhanViens = await _context.nhan_vien
            .Where(nv => nv.xoa == 0)
            .ToDictionaryAsync(nv => nv.ma_nv, nv => nv);

        // Lấy các bản ghi phép tồn hiện có của năm
        var currentPhepTons = await _context.phep_ton
            .Where(p => p.year == year)
            .ToDictionaryAsync(p => p.ma_nv, p => p);

        // Các danh sách lưu trữ lỗi và thống kê
        var conversionErrorList = new List<(string ID, string InvalidValue)>();
        var notFoundInNhanVienList = new List<(string ID, int TotalRemain)>();
        int updatedCount = 0;
        // HashSet lưu lại các ID cập nhật thành công (có dữ liệu hợp lệ trong file)
        var fileValidIds = new HashSet<string>();

        // Xử lý file Excel import
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    string id = worksheet.Cells[row, 2].Text.Trim();
                    string totalRemainText = worksheet.Cells[row, 9].Text.Trim();

                    // Nếu không thể chuyển đổi sang số, lưu vào danh sách lỗi chuyển đổi
                    if (!int.TryParse(totalRemainText, NumberStyles.Any, CultureInfo.InvariantCulture, out int totalRemain))
                    {
                        conversionErrorList.Add((id, totalRemainText));
                        continue;
                    }

                    // Nếu ID không tồn tại trong bảng nhân viên thì lưu vào danh sách lỗi
                    if (!nhanViens.ContainsKey(id))
                    {
                        notFoundInNhanVienList.Add((id, totalRemain));
                        continue;
                    }

                    // Nếu bản ghi phép tồn của nhân viên đã có trong CSDL thì cập nhật
                    if (currentPhepTons.ContainsKey(id))
                    {
                        currentPhepTons[id].phep_ton = totalRemain;
                        updatedCount++;
                        fileValidIds.Add(id);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        // Tạo danh sách các nhân viên không được cập nhật (không có trong file hoặc có lỗi chuyển đổi)
        var notUpdatedList = new List<(string ID, int CurrentPhepTon)>();
        foreach (var kv in currentPhepTons)
        {
            if (!fileValidIds.Contains(kv.Key))
            {
                notUpdatedList.Add((kv.Key, kv.Value.phep_ton));
            }
        }

        // Tạo file Excel báo cáo kết quả cập nhật
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Kết quả cập nhật");
            worksheet.Cells[1, 1].Value = "Số nhân viên cập nhật thành công:";
            worksheet.Cells[1, 2].Value = updatedCount;
            // Tổng số nhân viên trong file = số dòng cập nhật hợp lệ + lỗi chuyển đổi + ID không tồn tại trong bảng nhân viên
            int totalFileCount = fileValidIds.Count + conversionErrorList.Count + notFoundInNhanVienList.Count;
            worksheet.Cells[2, 1].Value = "Tổng số nhân viên trong file:";
            worksheet.Cells[2, 2].Value = totalFileCount;

            int currentRow = 4;

            // 1. Danh sách lỗi chuyển đổi
            if (conversionErrorList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên lỗi chuyển đổi phép tồn sang dạng số";
                worksheet.Cells[currentRow, 2].Value = "Giá trị không hợp lệ";
                currentRow++;
                foreach (var (ID, invalidValue) in conversionErrorList)
                {
                    worksheet.Cells[currentRow, 1].Value = ID;
                    worksheet.Cells[currentRow, 2].Value = invalidValue;
                    currentRow++;
                }
                currentRow++; // dòng trống
            }

            // 2. Danh sách các nhân viên có ID không tồn tại trong bảng nhân viên
            if (notFoundInNhanVienList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên có ID không tồn tại trong bảng nhân viên";
                worksheet.Cells[currentRow, 2].Value = "Total Remain";
                currentRow++;
                foreach (var (ID, totalRemain) in notFoundInNhanVienList)
                {
                    worksheet.Cells[currentRow, 1].Value = ID;
                    worksheet.Cells[currentRow, 2].Value = totalRemain;
                    currentRow++;
                }
                currentRow++; // dòng trống
            }

            // 3. Danh sách các nhân viên không được cập nhật (không có file hoặc lỗi chuyển đổi) với giá trị phép tồn giữ nguyên
            if (notUpdatedList.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Danh sách nhân viên không có file hoặc lỗi chuyển đổi (phép tồn giữ nguyên)";
                worksheet.Cells[currentRow, 2].Value = "Phép tồn hiện tại";
                currentRow++;
                foreach (var (ID, currentPhepTon) in notUpdatedList)
                {
                    worksheet.Cells[currentRow, 1].Value = ID;
                    worksheet.Cells[currentRow, 2].Value = currentPhepTon;
                    currentRow++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            var resultStream = new MemoryStream();
            package.SaveAs(resultStream);
            resultStream.Position = 0;
            return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KetQuaUpdatePhepTonExcel.xlsx");
        }
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetPhepTon([FromQuery] string ma_nv, [FromQuery] string year)
    {
        var result = await _context.phep_ton
            .FirstOrDefaultAsync(p => p.ma_nv == ma_nv && p.year == year);

        if (result == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu" });

        return Ok(result);
    }
    [Authorize]
    [HttpGet("years")]
    public async Task<IActionResult> GetDistinctYears()
    {
        var years = await _context.phep_ton
            .Select(p => p.year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        if (!years.Any())
            return NotFound(new { message = "Không có dữ liệu năm" });

        return Ok(years);
    }
    [Authorize]
    [HttpGet("get-phep-ton")]
    public async Task<IActionResult> GetDsPhepTon(string year, string searchTerm, int page = 1, int pageSize = 15)
    {
        var query = from pt in _context.phep_ton
                        // join nv in _context.nhan_vien on pt.ma_nv equals nv.ma_nv into nvJoin
                        // from nv in nvJoin.DefaultIfEmpty()
                    join nv in _context.nhan_vien on pt.ma_nv equals nv.ma_nv
                    where nv.xoa != 1
                    join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtJoin
                    from vt in vtJoin.DefaultIfEmpty()
                    join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpJoin
                    from bp in bpJoin.DefaultIfEmpty()
                    where pt.year == year
                    select new
                    {
                        MaNv = pt.ma_nv,
                        TenNhanVien = nv.full_name,
                        GioiTinh = nv.gioi_tinh,
                        TenViTri = vt != null ? vt.ten_vi_tri : null,
                        TenBoPhan = bp != null ? bp.ten_bo_phan : null,
                        CongViec = nv.cong_viec,
                        PhepTon = pt.phep_ton
                    };

        if (!string.IsNullOrEmpty(searchTerm) && searchTerm != "All")
        {
            query = query.Where(x =>
                x.MaNv.Contains(searchTerm) || x.TenNhanVien.Contains(searchTerm) ||
                (x.TenViTri != null && x.TenViTri.Contains(searchTerm)) ||
                (x.TenBoPhan != null && x.TenBoPhan.Contains(searchTerm))
            );
        }

        int totalCount = await query.CountAsync();

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            Items = items
        });
    }
    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<IActionResult> UpdatePhepTon([FromBody] UpdatePhepTonRequest request)
    {
        var phepTon = await _context.phep_ton
            .FirstOrDefaultAsync(p => p.ma_nv == request.MaNv && p.year == request.Year);

        if (phepTon == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu" });

        phepTon.phep_ton = request.PhepTon;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật thành công" });
    }
    [Authorize]
    [HttpGet("GetNghiPhepByMaNV/{ma_nv}")]
    public async Task<IActionResult> GetNghiPhepByMaNV(string ma_nv)
    {
        if (string.IsNullOrEmpty(ma_nv))
        {
            return BadRequest("Mã nhân viên không hợp lệ.");
        }

        var currentYear = DateTime.Now.Year;

        var nhanVien = await _context.nhan_vien.FirstOrDefaultAsync(nv => nv.ma_nv == ma_nv && nv.xoa != 1);
        if (nhanVien == null)
        {
            return NotFound("Không tìm thấy nhân viên.");
        }

        var nghiPhepList = await _context.nghi_phep
            .Where(np => np.ma_nv == nhanVien.ma_nv && np.trang_thai == "3")
            .ToListAsync();

        int totalNgayNghi = nghiPhepList
            .SelectMany(np => np.ngay_nghi.Split(','))
            .Select(dateStr => DateTime.TryParseExact(dateStr.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date) ? date : (DateTime?)null)
            .Where(date => date.HasValue && date.Value.Year == currentYear)
            .Count();

        var ngayNghiTheoLyDo = nghiPhepList
            .SelectMany(np => np.ngay_nghi.Split(',').Select(dateStr => new { dateStr, kyHieu = np.ky_hieu_ly_do }))
            .Select(item => new
            {
                Date = DateTime.TryParseExact(item.dateStr.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date) ? date : (DateTime?)null,
                KyHieuLyDo = item.kyHieu
            })
            .Where(item => item.Date.HasValue && item.Date.Value.Year == currentYear)
            .GroupBy(item => item.KyHieuLyDo)
            .Select(g => new
            {
                LyDo = g.Key,
                SoNgay = g.Count()
            })
            .ToList();

        var phepTon = await _context.phep_ton
            .Where(pt => pt.ma_nv == ma_nv && pt.year == currentYear.ToString())
            .Select(pt => pt.phep_ton)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            MaNV = nhanVien.ma_nv,
            TotalNgayNghi = totalNgayNghi,
            ChiTietNgayNghi = ngayNghiTheoLyDo,
            PhepTon = phepTon
        });
    }
}
public class UpdatePhepTonRequest
{
    public string MaNv { get; set; }
    public string Year { get; set; }
    public int PhepTon { get; set; }
}
