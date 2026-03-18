using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using Microsoft.AspNetCore.Authorization;
using API.Models;
using System.Security.Cryptography;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;

[Route("api/[controller]")]
[ApiController]
public class NhanVienController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private Dictionary<string, string> roleMapping = new Dictionary<string, string>
            {
                { "SP", "tao_phieu" },
                { "WK", "tao_phieu" },
                { "WKII", "tao_phieu" },
                { "SV", "tao_phieu,xu_ly" },
                { "EP", "tao_phieu" },
                { "TL", "tao_phieu,xu_ly" },
                { "DM_1", "tao_phieu,xu_ly,bao_bp_cao_bo_phan" },
                { "SDM", "tao_phieu,xu_ly,bao_bp_cao_bo_phan" },
                { "SM", "tao_phieu,xu_ly,bao_bp_cao_bo_phan" },
                {"MS", "tao_phieu,xu_ly,bao_cao"},
                { "DTL", "tao_phieu,xu_ly" },
                { "TL_1", "tao_phieu,xu_ly,bao_bp_cao_bo_phan" },
                { "GD", "tao_phieu,xu_ly" },
                { "DM", "tao_phieu,xu_ly,bao_bp_cao_bo_phan" },
                {"PMTL", "tao_phieu,xu_ly"},
                {"PME", "tao_phieu"},
                {"PMS", "tao_phieu,bao_bp_cao_bo_phan"},
                {"TTL", "tao_phieu,xu_ly"},
                {"TE", "tao_phieu,xu_ly"},
                {"TS", "tao_phieu"},
                {"TF", "tao_phieu,xu_ly,bao_bp_cao_bo_phan"}
            };

    public NhanVienController(ApplicationDbContext context)
    {
        _context = context;
    }
    // [HttpPost("UpdateWorkingPositionFromExcel")]
    // public async Task<IActionResult> UpdateWorkingPositionFromExcel(IFormFile file)
    // {
    //     if (file == null || file.Length <= 0)
    //     {
    //         return BadRequest("File rỗng hoặc không hợp lệ.");
    //     }
    //     UpdateResult updateResult = new UpdateResult();

    //     ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    //     using (var stream = new MemoryStream())
    //     {
    //         await file.CopyToAsync(stream);
    //         stream.Position = 0;

    //         using (var package = new ExcelPackage(stream))
    //         {
    //             var worksheet = package.Workbook.Worksheets.FirstOrDefault();
    //             if (worksheet == null)
    //                 return BadRequest("Không tìm thấy worksheet nào trong file Excel.");

    //             int rowCount = worksheet.Dimension.Rows;
    //             for (int row = 2; row <= rowCount; row++)
    //             {
    //                 updateResult.Total++;
    //                 string maNv = worksheet.Cells[row, 2].Text?.Trim();
    //                 string congViec = worksheet.Cells[row, 8].Text?.Trim();

    //                 if (string.IsNullOrEmpty(maNv))
    //                 {
    //                     updateResult.Failed++;
    //                     updateResult.FailedList.Add(new FailedEmployee
    //                     {
    //                         MaNv = maNv,
    //                         CongViec = congViec,
    //                         ErrorMessage = "Thiếu mã nhân viên."
    //                     });
    //                     continue;
    //                 }

    //                 try
    //                 {
    //                     var nhanVien = await _context.nhan_vien.FirstOrDefaultAsync(nv => nv.ma_nv == maNv);
    //                     if (nhanVien == null)
    //                     {
    //                         updateResult.Failed++;
    //                         updateResult.FailedList.Add(new FailedEmployee
    //                         {
    //                             MaNv = maNv,
    //                             CongViec = congViec,
    //                             ErrorMessage = "Mã nhân viên không tồn tại."
    //                         });
    //                         continue;
    //                     }

    //                     nhanVien.cong_viec = congViec;
    //                     await _context.SaveChangesAsync();
    //                     updateResult.Success++;
    //                 }
    //                 catch (Exception ex)
    //                 {
    //                     updateResult.Failed++;
    //                     updateResult.FailedList.Add(new FailedEmployee
    //                     {
    //                         MaNv = maNv,
    //                         CongViec = congViec,
    //                         ErrorMessage = ex.Message
    //                     });
    //                 }
    //             }
    //         }
    //     }

    //     using (var resultPackage = new ExcelPackage())
    //     {
    //         var ws = resultPackage.Workbook.Worksheets.Add("Import Result");

    //         ws.Cells["A1"].Value = "Số nhân viên trong file:";
    //         ws.Cells["B1"].Value = updateResult.Total;
    //         ws.Cells["A2"].Value = "Số nhân viên update thành công:";
    //         ws.Cells["B2"].Value = updateResult.Success;
    //         ws.Cells["A3"].Value = "Số nhân viên update không thành công:";
    //         ws.Cells["B3"].Value = updateResult.Failed;
    //         ws.Cells.AutoFitColumns();

    //         int startRow = 5;
    //         if (updateResult.FailedList.Count() > 0)
    //         {
    //             ws.Cells[startRow, 1].Value = "Mã nhân viên";
    //             ws.Cells[startRow, 2].Value = "Công việc";
    //             ws.Cells[startRow, 3].Value = "Lỗi";
    //         }

    //         int currentRow = startRow + 1;
    //         foreach (var failedEmp in updateResult.FailedList)
    //         {
    //             ws.Cells[currentRow, 1].Value = failedEmp.MaNv;
    //             ws.Cells[currentRow, 2].Value = failedEmp.CongViec;
    //             ws.Cells[currentRow, 3].Value = failedEmp.ErrorMessage;
    //             currentRow++;
    //         }

    //         var resultStream = new MemoryStream();
    //         resultPackage.SaveAs(resultStream);
    //         resultStream.Position = 0;
    //         string fileName = $"ImportResult-{DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}.xlsx";

    //         return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    //     }
    // }
    // [Authorize(Roles = "admin")]
    // [HttpGet("list-role")]
    // public async Task<ActionResult<PaginatedResponse<NhanVienDto>>> GetRoleListNhanVien(string searchTerm = "Nội dung tìm kiếm", int pageNumber = 1, int pageSize = 10)
    // {
    //     var query = from nv in _context.nhan_vien
    //                 where nv.xoa != 1
    //                 join u in _context.user on nv.ma_nv equals u.ma_nv
    //                 join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bps
    //                 from bp in bps.DefaultIfEmpty()
    //                 join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vts
    //                 from vt in vts.DefaultIfEmpty()
    //                 select new NhanVienDto
    //                 {
    //                     id = nv.id,
    //                     ten_bo_phan = bp.ten_bo_phan,
    //                     ma_nv = nv.ma_nv,
    //                     full_name = nv.full_name,
    //                     vi_tri = vt != null ? vt.ten_vi_tri : null,
    //                     role = u.role
    //                 };

    //     if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "Nội dung tìm kiếm")
    //     {
    //         query = query.Where(nv => nv.ten_bo_phan.Contains(searchTerm) ||
    //                                    nv.full_name.Contains(searchTerm) ||
    //                                    nv.ma_nv.Contains(searchTerm));
    //     }
    //     query = query.OrderByDescending(nv => nv.id);
    //     var totalCount = await query.CountAsync();
    //     var items = await query.Skip((pageNumber - 1) * pageSize)
    //                             .Take(pageSize)
    //                             .ToListAsync();

    //     return Ok(new PaginatedResponse<NhanVienDto>
    //     {
    //         TotalCount = totalCount,
    //         Items = items
    //     });
    // }
    // [Authorize(Roles = "tao_phieu")]
    // [HttpGet("nhan-vien_detail/{ma_nv}")]
    // public async Task<IActionResult> GetNhanVienDetail(string ma_nv)
    // {
    //     var query = from nv in _context.nhan_vien
    //                 where nv.xoa != 1
    //                 join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpJoin
    //                 from bp in bpJoin.DefaultIfEmpty()
    //                 join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtJoin
    //                 from vt in vtJoin.DefaultIfEmpty()
    //                 where nv.ma_nv == ma_nv
    //                 select new
    //                 {
    //                     nv.id,
    //                     nv.ma_nv,
    //                     nv.full_name,
    //                     bp.ten_bo_phan,
    //                     vt.ten_vi_tri,
    //                     nv.cong_viec
    //                 };
    //     var result = await query.FirstOrDefaultAsync();
    //     if (result == null)
    //     {
    //         return NotFound("Nhân viên không tồn tại.");
    //     }
    //     return Ok(result);
    // }

    // [Authorize(Roles = "admin")]
    // [HttpPut("update-role")]
    // public async Task<IActionResult> UpdateRole([FromBody] UpdateUserRoleDto updateUserRoleDto)
    // {
    //     if (updateUserRoleDto == null || string.IsNullOrEmpty(updateUserRoleDto.ma_nv))
    //     {
    //         return BadRequest("Mã nhân viên không hợp lệ.");
    //     }

    //     var user = await _context.user.FirstOrDefaultAsync(u => u.ma_nv == updateUserRoleDto.ma_nv);
    //     if (user == null)
    //     {
    //         return NotFound("Người dùng không tồn tại.");
    //     }

    //     user.role = updateUserRoleDto.role;

    //     await _context.SaveChangesAsync();

    //     return NoContent();
    // }
    // [Authorize(Roles = "tao_phieu")]
    // [HttpGet("nhan_vien_cung_bo_phan")]
    // public async Task<ActionResult<object>> GetNhanVienCungBoPhan([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "All")
    // {
    //     var ten_bo_phan = User.FindFirst("ten_bo_phan").Value;
    //     var query = from nv in _context.nhan_vien
    //                 where nv.xoa != 1
    //                 join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
    //                 from bp in bpGroup.DefaultIfEmpty()
    //                 where bp.ten_bo_phan == ten_bo_phan
    //                 select new
    //                 {
    //                     nv.id,
    //                     nv.ma_nv,
    //                     nv.full_name,
    //                     ma_ten_nv = nv.ma_nv + " - " + nv.full_name,
    //                     ten_bo_phan = bp != null ? bp.ten_bo_phan : null
    //                 };
    //     if (!string.IsNullOrEmpty(searchTerm) && searchTerm != "All")
    //     {
    //         query = query.Where(nv => nv.ma_nv.Contains(searchTerm) ||
    //                                    nv.full_name.Contains(searchTerm));
    //     }
    //     var totalRecords = await query.CountAsync();
    //     var employees = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

    //     return Ok(new
    //     {
    //         TotalRecords = totalRecords,
    //         PageIndex = pageNumber,
    //         PageSize = pageSize,
    //         Employees = employees,
    //     });
    // }

    // [HttpGet]
    // public async Task<ActionResult<object>> GetNhanVien([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = "Nội dung tìm kiếm")
    // {
    //     var query = from nv in _context.nhan_vien
    //                 where nv.xoa != 1
    //                 join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
    //                 from bp in bpGroup.DefaultIfEmpty()
    //                 join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtGroup
    //                 from vt in vtGroup.DefaultIfEmpty()
    //                 select new
    //                 {
    //                     nv.id,
    //                     nv.ma_nv,
    //                     nv.full_name,
    //                     nv.gioi_tinh,
    //                     nv.ma_vi_tri,
    //                     vi_tri = vt != null ? vt.ten_vi_tri : null,
    //                     nv.cong_viec,
    //                     nv.bo_phan_id,
    //                     nv.email,
    //                     ten_bo_phan = bp != null ? bp.ten_bo_phan : null
    //                 };
    //     if (!string.IsNullOrEmpty(searchTerm) && searchTerm != "Nội dung tìm kiếm")
    //     {
    //         query = query.Where(nv => nv.ma_nv.Contains(searchTerm) ||
    //                                    nv.full_name.Contains(searchTerm) ||
    //                                    nv.ten_bo_phan.Contains(searchTerm));
    //     }
    //     query = query.OrderByDescending(nv => nv.id);
    //     var totalRecords = await query.CountAsync();
    //     var employees = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

    //     return Ok(new
    //     {
    //         TotalRecords = totalRecords,
    //         PageIndex = pageNumber,
    //         PageSize = pageSize,
    //         Employees = employees
    //     });
    // }

    // [Authorize(Roles = "admin")]
    // [HttpPut("{id}")]
    // public async Task<IActionResult> UpdateNhanVien(ulong id, [FromBody] UpdateNhanVienDto nhanVienDto)
    // {
    //     var nhanVien = await _context.nhan_vien.FindAsync(id);

    //     if (nhanVien == null)
    //     {
    //         return NotFound(new { Message = "Nhân viên không tồn tại." });
    //     }

    //     nhanVien.full_name = nhanVienDto.full_name ?? nhanVien.full_name;
    //     nhanVien.gioi_tinh = nhanVienDto.gioi_tinh ?? nhanVien.gioi_tinh;
    //     if (nhanVienDto.bo_phan_id != 0)
    //     {
    //         nhanVien.bo_phan_id = nhanVienDto.bo_phan_id ?? nhanVien.bo_phan_id;
    //     }
    //     if (nhanVienDto.cong_viec != "defaut")
    //     {
    //         nhanVien.cong_viec = nhanVienDto.cong_viec ?? nhanVien.cong_viec;
    //     }
    //     if (nhanVienDto.email != "defaut")
    //     {
    //         nhanVien.email = nhanVienDto.email ?? nhanVien.email;
    //     }
    //     if (!string.IsNullOrEmpty(nhanVienDto.ma_vi_tri) && nhanVienDto.ma_vi_tri != nhanVien.ma_vi_tri)
    //     {
    //         nhanVien.ma_vi_tri = nhanVienDto.ma_vi_tri;

    //         if (this.roleMapping.TryGetValue(nhanVienDto.ma_vi_tri, out var newRole))
    //         {
    //             var user = await _context.user.FirstOrDefaultAsync(u => u.id == nhanVien.id);

    //             if (user != null)
    //             {
    //                 user.role = newRole;
    //                 if (nhanVien.bo_phan_id == 2)
    //                 {
    //                     var additionalRoles = "admin,bao_cao";
    //                     var currentRoles = new HashSet<string>(user.role.Split(','));
    //                     var newRoles = new HashSet<string>(additionalRoles.Split(','));
    //                     currentRoles.UnionWith(newRoles);
    //                     user.role = string.Join(",", currentRoles);
    //                 }
    //             }
    //         }
    //     }
    //     try
    //     {
    //         await _context.SaveChangesAsync();
    //         return Ok(new { Message = "Cập nhật thông tin nhân viên thành công." });
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(new { Message = "Có lỗi xảy ra khi cập nhật thông tin nhân viên.", Error = ex.Message });
    //     }
    // }
    // [Authorize(Roles = "admin")]
    // [HttpPost]
    // public async Task<ActionResult<CreatNhanVienResponse>> CreateNhanVien(NhanVien newNhanVien)
    // {
    //     bool isExist = await _context.nhan_vien.AnyAsync(nv => nv.ma_nv == newNhanVien.ma_nv);
    //     if (isExist)
    //     {
    //         return BadRequest(new { code = 2, message = "Mã nhân viên đã tồn tại!" });
    //     }
    //     _context.nhan_vien.Add(newNhanVien);
    //     await _context.SaveChangesAsync();
    //     string password = GenerateRandomPassword(6);
    //     this.roleMapping.TryGetValue(newNhanVien.ma_vi_tri, out var newRole);
    //     var user = new User
    //     {
    //         id_nv = 0,
    //         ma_nv = newNhanVien.ma_nv,
    //         password = HashPassword(password),
    //         role = newRole
    //     };
    //     var phep_ton = new PhepTon
    //     {
    //         id = 0,
    //         ma_nv = newNhanVien.ma_nv,
    //         year = DateTime.Now.Year.ToString(),
    //         phep_ton = 0
    //     };
    //     _context.phep_ton.Add(phep_ton);
    //     if (newNhanVien.bo_phan_id == 2)
    //     {
    //         var additionalRoles = "admin,bao_cao";
    //         var currentRoles = new HashSet<string>(user.role.Split(','));
    //         var newRoles = new HashSet<string>(additionalRoles.Split(','));
    //         currentRoles.UnionWith(newRoles);
    //         user.role = string.Join(",", currentRoles);
    //     }
    //     _context.user.Add(user);
    //     await _context.SaveChangesAsync();

    //     var response = new CreatNhanVienResponse
    //     {
    //         ma_nv = newNhanVien.ma_nv,
    //         id = newNhanVien.id,
    //         password = password
    //     };
    //     return Ok(response);
    // }
    // [Authorize(Roles = "admin")]
    // [HttpDelete("{id}")]
    // public async Task<IActionResult> DeleteNhanVien(ulong id)
    // {
    //     var nhanVien = await _context.nhan_vien.FindAsync(id);

    //     if (nhanVien == null)
    //     {
    //         return NotFound(new { Message = "Nhân viên không tồn tại." });
    //     }
    //     nhanVien.xoa = 1;
    //     try
    //     {
    //         await _context.SaveChangesAsync();
    //         return Ok(new { Message = "Xóa nhân viên thành công." });
    //     }
    //     catch
    //     {
    //         return Conflict(new { Message = "Có lỗi xảy ra khi xóa nhân viên." });
    //     }
    // }
    // private string GenerateRandomPassword(int length)
    // {
    //     const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz123456789";
    //     var random = new Random();
    //     return new string(Enumerable.Repeat(chars, length)
    //         .Select(s => s[random.Next(s.Length)]).ToArray());
    // }
    // private string HashPassword(string password)
    // {
    //     using (SHA256 sha256 = SHA256.Create())
    //     {
    //         byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    //         StringBuilder builder = new StringBuilder();
    //         foreach (byte b in bytes)
    //         {
    //             builder.Append(b.ToString("x2"));
    //         }
    //         return builder.ToString();
    //     }
    // }
    // [Authorize(Roles = "admin")]
    // [HttpGet("xuat-ds-nhan-vien")]
    // public async Task<IActionResult> XuatDsNhanVien()
    // {
    //     var currentYear = DateTime.Now.Year.ToString();
    //     var positionMapping = new Dictionary<string, string>
    //     {
    //         { "CHUYEN_VIEN", "Chuyên viên" },
    //         { "CONG_NHAN", "Công nhân" },
    //         { "CONG_NHAN_II", "Công nhân II" },
    //         { "GIAM_SAT", "Giám sát" },
    //         { "NHAN_VIEN", "Nhân viên" },
    //         { "NHOM_TRUONG", "Nhóm trưởng" },
    //         { "PHO_PHONG", "Phó phòng" },
    //         { "PHO_QUAN_LY_CAP_CAO", "Phó quản lý cấp cao" },
    //         { "QUAN_LY_CAP_CAO", "Quản lý cấp cao" },
    //         { "TO_PHO", "Tổ phó" },
    //         { "TO_TRUONG", "Tổ trưởng" },
    //         { "TONG_GIAM_DOC", "Tổng giám đốc" },
    //         { "TRUONG_PHONG", "Trưởng phòng" },
    //         { "GIAM_SAT_PHONG_QUAN_LY", "Giám sát phòng quản lý" },
    //         {"NHOM_TRUONG_QUAN_LY_SX", "Nhóm trưởng quản lý sản xuất"},
    //         {"NHAN_VIEN_QUAN_LY_SX", "Nhân viên quản lý sản xuất"},
    //         {"CHUYEN_VIEN_QUAN_LY_SX", "Chuyên viên quản lý sản xuất"},
    //         {"NHOM_TRUONG_KY_THUAT", "Nhóm trưởng kỹ thuật"},
    //         {"NHAN_VIEN_KY_THUAT", "Nhân viên kỹ thuật"},
    //         {"CHUYEN_VIEN_KY_THUAT", "Chuyên viên kỹ thuật"},
    //         {"TO_TRUONG_KY_THUAT", "Tổ trưởng kỹ thuật"}
    //     };

    //     var query = from nv in _context.nhan_vien
    //                 where nv.xoa != 1
    //                 join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpJoin
    //                 from bp in bpJoin.DefaultIfEmpty()
    //                 join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtJoin
    //                 from vt in vtJoin.DefaultIfEmpty()
    //                 join pt in _context.phep_ton on nv.ma_nv equals pt.ma_nv into ptJoin
    //                 from pt in ptJoin.DefaultIfEmpty()
    //                 where pt.year == currentYear
    //                 orderby nv.ma_nv
    //                 select new
    //                 {
    //                     nv.id,
    //                     nv.ma_nv,
    //                     nv.full_name,
    //                     nv.gioi_tinh,
    //                     ten_vi_tri = positionMapping.ContainsKey(vt.ten_vi_tri) ? positionMapping[vt.ten_vi_tri] : vt.ten_vi_tri, // Map tên vị trí
    //                     bp.ten_bo_phan,
    //                     nv.cong_viec,
    //                     pt.phep_ton,
    //                     nv.email
    //                 };

    //     var result = query.ToList();

    //     if (result == null || !result.Any())
    //     {
    //         return NotFound("Nhân viên không tồn tại.");
    //     }
    //     using var package = new ExcelPackage();
    //     var worksheet = package.Workbook.Worksheets.Add("Danh sách nhân viên");

    //     var headers = new string[]
    //     {
    //     "STT", "ID", "Họ và tên", "Nam", "Nữ", "Chức vụ", "Bộ phận", "Vị trí", $"Phép tồn đến hết {currentYear}", "Mail"
    //     };

    //     for (int i = 0; i < headers.Length; i++)
    //     {
    //         worksheet.Cells[1, i + 1].Value = headers[i];
    //         worksheet.Cells[1, i + 1].Style.Font.Bold = true;
    //         worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //     }

    //     int row = 2;
    //     int stt = 1;
    //     foreach (var item in result)
    //     {
    //         worksheet.Cells[row, 1].Value = stt++;
    //         worksheet.Cells[row, 2].Value = item.ma_nv;
    //         worksheet.Cells[row, 3].Value = item.full_name;
    //         worksheet.Cells[row, 4].Value = item.gioi_tinh == "Nam" ? "x" : "";
    //         worksheet.Cells[row, 5].Value = item.gioi_tinh == "Nữ" ? "x" : "";
    //         worksheet.Cells[row, 6].Value = item.ten_vi_tri;
    //         worksheet.Cells[row, 7].Value = item.ten_bo_phan;
    //         worksheet.Cells[row, 8].Value = item.cong_viec;
    //         worksheet.Cells[row, 9].Value = item.phep_ton;
    //         worksheet.Cells[row, 10].Value = item.email;
    //         row++;
    //     }

    //     worksheet.Cells.AutoFitColumns();
    //     var stream = new MemoryStream();
    //     package.SaveAs(stream);
    //     stream.Position = 0;
    //     var fileName = $"Danh_sach_nhan_vien_{DateTime.Now:dd/MM/yyyy}.xlsx";
    //     return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    // }
    // Import nhân viên từ file Excel
    // [Authorize(Roles = "admin")]
    // [HttpPost("ImportNhanVien")]
    // public async Task<IActionResult> ImportNhanVien(IFormFile file)
    // {
    //     if (file == null || file.Length == 0)
    //     {
    //         return BadRequest("File không tồn tại hoặc rỗng.");
    //     }

    //     using var package = new ExcelPackage(file.OpenReadStream());
    //     var worksheet = package.Workbook.Worksheets.FirstOrDefault();
    //     if (worksheet == null)
    //     {
    //         return BadRequest("Không tìm thấy worksheet trong file Excel.");
    //     }

    //     int rowCount = worksheet.Dimension.End.Row;
    //     int colCount = worksheet.Dimension.End.Column;

    //     // Thêm 2 cột kết quả: "Trạng thái import" và "Mật khẩu đăng nhập"
    //     worksheet.Cells[1, colCount + 1].Value = "Trạng thái import";
    //     worksheet.Cells[1, colCount + 1].Style.Font.Bold = true;
    //     worksheet.Cells[1, colCount + 2].Value = "Mật khẩu đăng nhập";
    //     worksheet.Cells[1, colCount + 2].Style.Font.Bold = true;

    //     // Mapping vị trí hiển thị sang mã vị trí
    //     var positionMapping = new Dictionary<string, string>
    //     {
    //         { "SP", "Chuyên viên" },
    //         { "WK", "Công nhân" },
    //         { "WKII", "Công nhân II" },
    //         { "SV", "Giám sát" },
    //         { "EP", "Nhân viên" },
    //         { "TL", "Nhóm trưởng" },
    //         { "DM_1", "Phó phòng" },
    //         { "SDM", "Phó quản lý cấp cao" },
    //         { "SM", "Quản lý cấp cao" },
    //         { "DTL", "Tổ phó" },
    //         { "TL_1", "Tổ trưởng" },
    //         { "GD", "Tổng giám đốc" },
    //         { "DM", "Trưởng phòng" },
    //         { "MS", "Giám sát phòng quản lý" },

    //         {"PMTL", "Nhóm trưởng quản lý sản xuất"},
    //         {"PME", "Nhân viên quản lý sản xuất"},
    //         {"PMS", "Chuyên viên quản lý sản xuất"},
    //         {"TTL", "Nhóm trưởng kỹ thuật"},
    //         {"TE", "Nhân viên kỹ thuật"},
    //         {"TS", "Chuyên viên kỹ thuật"},
    //         {"TF", "Tổ trưởng kỹ thuật"}
    //     };

    //     // Reverse mapping: hiển thị -> mã vị trí
    //     var reversePositionMapping = positionMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    //     // Lấy danh sách bộ phận để mapping từ tên bộ phận sang bo_phan_id
    //     var boPhanList = await _context.bo_phan.ToListAsync();
    //     var boPhanMapping = boPhanList.ToDictionary(bp => bp.ten_bo_phan, bp => bp.id);

    //     // Duyệt qua từng dòng (bắt đầu từ dòng 2, vì dòng 1 là header)
    //     for (int row = 2; row <= rowCount; row++)
    //     {
    //         // Đọc dữ liệu từ file Excel
    //         string ma_nv = worksheet.Cells[row, 2].Text.Trim();
    //         string full_name = worksheet.Cells[row, 3].Text.Trim();

    //         string gioi_tinh = "";
    //         if (worksheet.Cells[row, 4].Text.Trim().Equals("x", StringComparison.OrdinalIgnoreCase))
    //             gioi_tinh = "Nam";
    //         else if (worksheet.Cells[row, 5].Text.Trim().Equals("x", StringComparison.OrdinalIgnoreCase))
    //             gioi_tinh = "Nữ";

    //         string chucVuDisplay = worksheet.Cells[row, 6].Text.Trim();
    //         string ma_vi_tri = "";
    //         string errorMapping = "";
    //         if (string.IsNullOrEmpty(gioi_tinh))
    //         {
    //             errorMapping += "Thiếu thông tin giới tính. ";
    //         }
    //         if (string.IsNullOrEmpty(full_name))
    //         {
    //             errorMapping += "Thiếu thông tin tên nhân viên";
    //         }
    //         if (reversePositionMapping.ContainsKey(chucVuDisplay))
    //         {
    //             ma_vi_tri = reversePositionMapping[chucVuDisplay];
    //         }
    //         else
    //         {
    //             errorMapping += $"Không tìm thấy chức vụ tương ứng. ";
    //             ma_vi_tri = "";
    //         }
    //         string ten_bo_phan = worksheet.Cells[row, 7].Text.Trim();
    //         ulong? bo_phan_id = 0;

    //         // Kiểm tra mapping cho Bộ phận
    //         if (string.IsNullOrEmpty(ten_bo_phan))
    //         {
    //             bo_phan_id = null;
    //         }
    //         else
    //         {
    //             if (boPhanMapping.ContainsKey(ten_bo_phan))
    //             {
    //                 bo_phan_id = boPhanMapping[ten_bo_phan];
    //             }
    //             else
    //             {
    //                 errorMapping += $"Không tìm thấy bộ phận tương ứng. ";
    //                 bo_phan_id = 0;
    //             }
    //         }

    //         string cong_viec = worksheet.Cells[row, 8].Text.Trim();

    //         // Đọc giá trị "Phép tồn" từ cột 9
    //         string phepTonStr = worksheet.Cells[row, 9].Text.Trim();
    //         int phepTonValue = 0;
    //         if (!string.IsNullOrEmpty(phepTonStr))
    //         {
    //             int.TryParse(phepTonStr, out phepTonValue);
    //         }
    //         else
    //         {
    //             errorMapping += "Không có dữ liệu phép tồn";
    //         }

    //         string email = worksheet.Cells[row, 10].Text.Trim();
    //         // Nếu có lỗi mapping, ghi lỗi vào cột "Trạng thái import" và bỏ qua dòng hiện tại
    //         if (!string.IsNullOrEmpty(errorMapping))
    //         {
    //             worksheet.Cells[row, colCount + 1].Value = errorMapping;
    //             worksheet.Cells[row, colCount + 2].Value = "";
    //             continue;
    //         }

    //         // Tạo đối tượng NhanVien mới từ dữ liệu nhập
    //         var newNhanVien = new NhanVien
    //         {
    //             ma_nv = ma_nv,
    //             full_name = full_name,
    //             gioi_tinh = gioi_tinh,
    //             ma_vi_tri = ma_vi_tri,
    //             bo_phan_id = bo_phan_id,
    //             cong_viec = cong_viec,
    //             email = email
    //         };

    //         try
    //         {
    //             // Kiểm tra trùng mã nhân viên
    //             bool isExist = await _context.nhan_vien.AnyAsync(nv => nv.ma_nv == newNhanVien.ma_nv);
    //             if (isExist)
    //             {
    //                 worksheet.Cells[row, colCount + 1].Value = "Mã nhân viên đã tồn tại!";
    //                 worksheet.Cells[row, colCount + 2].Value = "";
    //                 continue;
    //             }

    //             _context.nhan_vien.Add(newNhanVien);
    //             await _context.SaveChangesAsync();

    //             // Sinh mật khẩu ngẫu nhiên
    //             string password = GenerateRandomPassword(6);
    //             this.roleMapping.TryGetValue(newNhanVien.ma_vi_tri, out var newRole);
    //             var user = new User
    //             {
    //                 id_nv = newNhanVien.id,
    //                 ma_nv = newNhanVien.ma_nv,
    //                 password = HashPassword(password),
    //                 role = newRole
    //             };

    //             // Tạo bảng phép tồn cho nhân viên với giá trị đọc từ file Excel
    //             var phep_ton = new PhepTon
    //             {
    //                 id = 0,
    //                 ma_nv = newNhanVien.ma_nv,
    //                 year = DateTime.Now.Year.ToString(),
    //                 phep_ton = phepTonValue
    //             };
    //             _context.phep_ton.Add(phep_ton);

    //             // Nếu bộ phận có id == 2 thì thêm role admin, báo cáo
    //             if (newNhanVien.bo_phan_id == 2)
    //             {
    //                 var additionalRoles = "admin,bao_cao";
    //                 var currentRoles = new HashSet<string>(user.role.Split(','));
    //                 var newRoles = new HashSet<string>(additionalRoles.Split(','));
    //                 currentRoles.UnionWith(newRoles);
    //                 user.role = string.Join(",", currentRoles);
    //             }
    //             _context.user.Add(user);
    //             await _context.SaveChangesAsync();

    //             // Ghi kết quả import thành công và mật khẩu vào file Excel output
    //             worksheet.Cells[row, colCount + 1].Value = "Thành công";
    //             worksheet.Cells[row, colCount + 2].Value = password;
    //         }
    //         catch (Exception ex)
    //         {
    //             worksheet.Cells[row, colCount + 1].Value = "Lỗi: " + ex.Message;
    //             worksheet.Cells[row, colCount + 2].Value = "";
    //         }
    //     }

    //     // Lưu file kết quả vào MemoryStream và trả về cho client
    //     var outputStream = new MemoryStream();
    //     package.SaveAs(outputStream);
    //     outputStream.Position = 0;
    //     string outputFileName = $"KetQuaImportNha nVien_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";
    //     return File(outputStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outputFileName);
    // }

}
public class PaginatedResponse<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; }
}
public class FailedEmployee
{
    public string MaNv { get; set; }
    public string CongViec { get; set; }
    public string ErrorMessage { get; set; }
}

public class UpdateResult
{
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public List<FailedEmployee> FailedList { get; set; } = new List<FailedEmployee>();
}
