using API.Common;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiemKeHoachController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public DiemKeHoachController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Tra cứu danh sách dịch vụ.
        /// </summary>
        /// <returns>Trả về danh sách dịch vụ.</returns>
        [Authorize]
        [HttpPost("ds_diemkehoach")]
        public async Task<ActionResult<object>> GetDsDiemKeHoach([FromBody] DsDiemKeHaochRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var pageNumber = Math.Max(1, req.PageNumber);
                var pageSize = Math.Clamp(req.PageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var whereBuilder = new System.Text.StringBuilder($" WHERE a.THANGNAM = {req.ThangNam}");
                var paramList = new List<DbParameter>();

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                if (req.KhoaId != null && req.KhoaId != 0)
                {
                    whereBuilder.Append($" AND a.KHOAID = {req.KhoaId}");
                }

                if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
                {
                    var p = tempCmd.CreateParameter();
                    p.ParameterName = "@search";
                    p.Value = $"%{req.SearchTerm}%";
                    paramList.Add(p);

                    whereBuilder.Append(" AND a.BACSI LIKE @search");
                }

                var sql = $"SELECT * FROM `{dbData}`.BC_DIEMKEHOACH a " 
                    + whereBuilder.ToString() + $" ORDER BY a.THANGNAM, a.KHOAID LIMIT {pageSize} OFFSET {offset}" ;

                var dsDichVu = await _context.dto_dichvu
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(1) FROM `{dbData}`.BC_DIEMKEHOACH a" + whereBuilder.ToString();

                    cmd.Parameters.Clear();
                    foreach (var p in paramList)
                    {
                        var np = cmd.CreateParameter();
                        np.ParameterName = p.ParameterName;
                        np.Value = p.Value;
                        cmd.Parameters.Add(np);
                    }

                    var scalar = await cmd.ExecuteScalarAsync();
                    totalRecords = Convert.ToInt32(scalar ?? 0);
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    DsDichVu = dsDichVu,
                    id = req.IdLoaiDV
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật điểm kế hoạch.
        /// </summary>
        /// <returns>Trả về kq cập nhật.</returns>
        [Authorize]
        [HttpPut("cap-nhat-diemkehoach")]
        public async Task<ActionResult<object>> CapNhatDiemKeHoach([FromBody] CapNhatDiemKeHoachRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                if (req.DiemKeHoachId <= 0)
                    return BadRequest("DiemKeHoachId không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var conn = _context.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE `{dbData}`BC_DIEMKEHOACH " 
                    + " SET DIEM_KEHOACH = @DIEM_KEHOACH, SO_BUOITRUC = @SO_BUOITRUC, SO_BENHNHAN = @SO_BENHNHAN, "
                    + " DIEM_TRUC = @DIEM_TRUC, DIEM_TRUC_CC = @DIEM_TRUC_CC, DIEM_LAYMAU = @DIEM_LAYMAU "
                    + " WHERE DIEMKEHOACHID = @DIEMKEHOACHID";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@DIEM_KEHOACH";
                p1.Value = req.DiemKeHoach;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@SO_BUOITRUC";
                p2.Value = req.SoBuoiTruc;
                cmd.Parameters.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.ParameterName = "@SO_BENHNHAN";
                p3.Value = req.SoBenhNhan;
                cmd.Parameters.Add(p3);

                var DiemTruc = "";
                if (req.LoaiBacSi == '4') {
                    DiemTruc = req.SoBuoiTruc * 12;
                } else {
                    DiemTruc = req.SoBuoiTruc * 8;
                }
                var p4 = cmd.CreateParameter();
                p4.ParameterName = "@DIEM_TRUC";
                p4.Value = DiemTruc;
                cmd.Parameters.Add(p4);

                var p5 = cmd.CreateParameter();
                p5.ParameterName = "@DIEM_TRUC_CC";
                p5.Value = req.DiemTrucCc;
                cmd.Parameters.Add(p5);

                var p6 = cmd.CreateParameter();
                p6.ParameterName = "@DIEM_LAYMAU";
                p6.Value = req.DiemLayMau;
                cmd.Parameters.Add(p6);

                var p7 = cmd.CreateParameter();
                p7.ParameterName = "@DIEMKEHOACHID";
                p7.Value = req.DiemKeHoachId;
                cmd.Parameters.Add(p7);

                var affectedRows = await cmd.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                    return NotFound(new { message = "Không tìm thấy bác sĩ cần cập nhật." });

                return Ok(new
                {
                    message = "Cập nhật thành công.",
                    affectedRows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
    public class DsDiemKeHaochRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? ThangNam { get; set; }
        public int? KhoaId { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class CapNhatDiemKeHoachRequest
    {
        public int DiemKeHoachId { get; set; }
        public int? DiemKeHoach { get; set; }
        public int? SoBuoiTruc { get; set; }
        public int? SoBenhNhan { get; set; }
        public int? LoaiBacSi { get; set; }
        public int? DiemTrucCc { get; set; }
        public int? DiemLayMau { get; set; }
    }
}