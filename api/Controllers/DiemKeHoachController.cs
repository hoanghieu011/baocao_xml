using API.Common;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
        public async Task<ActionResult<object>> GetDsDiemKeHoach([FromBody] DsDiemKeHoachRequest req)
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
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                var pageNumber = Math.Max(1, req.PageNumber);
                var pageSize = Math.Clamp(req.PageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();
                var sql1 = "SELECT b.*, org.ORG_NAME FROM his_common.org_officer b LEFT JOIN his_common.org_organization org ON org.ORG_ID = b.KHOAID WHERE b.STATUS=1 ";
                if(req.KhoaId!= null)
                {
                    sql1 += $"AND b.KHOAID = {req.KhoaId}";
                }
                if (req.SearchTerm != null && !req.SearchTerm.Equals("") ){
                    sql1 += $" AND b.OFFICER_NAME LIKE '%{req.SearchTerm}%' ";
                }
                var sql2 = $"SELECT * FROM  `{dbData}`.bc_diemkehoach a ";
                if(req.ThangNam != null)
                {
                    sql2 += $" WHERE a.THANGNAM = {req.ThangNam}";
                }
                var sql = $"SELECT ifnull(t2.DIEMKEHOACHID, 0) DIEMKEHOACHID, ifnull(t2.DIEM_KEHOACH, 0) DIEM_KEHOACH, ifnull(t2.SO_BUOITRUC, 0) SO_BUOITRUC, ifnull(t2.SO_BENHNHAN, 0) SO_BENHNHAN, ifnull(t2.DIEM_TRUC, 0) DIEM_TRUC, ifnull(t2.DIEM_TRUC_CC, 0) DIEM_TRUC_CC, ifnull(t2.DIEM_LAYMAU, 0) DIEM_LAYMAU, ifnull(t2.THANGNAM, 0) THANGNAM, t1.OFFICER_TYPE, t1.ORG_NAME KHOA, t1.OFFICER_NAME, t1.BACSIID, t1.KHOAID FROM " +
                    " ("+ sql1+ ") t1 "+
                    $" LEFT JOIN (" + sql2 + ") t2 ON t2.BACSIID = t1.BACSIID "+
                    $"LIMIT {pageSize} OFFSET {offset}";
                

                var dsDiemKeHoach = await _context.diemkehoach
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(1) FROM his_common.org_officer a WHERE a.KhoaId = {req.KhoaId}";

                    var scalar = await cmd.ExecuteScalarAsync();
                    totalRecords = Convert.ToInt32(scalar ?? 0);
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    DsDiemKeHoach = dsDiemKeHoach,
                    //id = req.IdLoaiDV
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
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var conn = _context.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE `{dbData}`.bc_diemkehoach " 
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
                if (req.OfficerType == '4') {
                    DiemTruc = req.SoBuoiTruc * 12 + "";
                } else {
                    DiemTruc = req.SoBuoiTruc * 8 + "";
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

        [Authorize]
        [HttpPost("them-moi-diemkehoach")]
        public async Task<ActionResult<object>> ThemDiemKeHoach([FromBody] ThemDiemKeHoachRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var conn = _context.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO `{dbData}`.bc_diemkehoach (KHOAID, BACSIID, BACSI, DIEM_KEHOACH, SO_BUOITRUC, SO_BENHNHAN, DIEM_TRUC, DIEM_TRUC_CC, DIEM_LAYMAU, THANGNAM, NGAYTAO)" +
                    "VALUES(@khoaId, @bacSiId, @bacSi, @diemKeHoach, @soBuoiTruc, @soBenhNhan, @diemTruc, @diemTrucCc, @diemLaymau, @thangNam, @ngayTao); SELECT LAST_INSERT_ID();";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@khoaId";
                p1.Value = req.KhoaId;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@bacSiId";
                p2.Value = req.BacSiId;
                cmd.Parameters.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.ParameterName = "@bacSi";
                p3.Value = req.BacSi;
                cmd.Parameters.Add(p3);

                var p4 = cmd.CreateParameter();
                p4.ParameterName = "@diemKeHoach";
                p4.Value = req.DiemKeHoach;
                cmd.Parameters.Add(p4);

                var p5 = cmd.CreateParameter();
                p5.ParameterName = "@soBuoiTruc";
                p5.Value = req.SoBuoiTruc;
                cmd.Parameters.Add(p5);

                var p6 = cmd.CreateParameter();
                p6.ParameterName = "@soBenhNhan";
                p6.Value = req.SoBenhNhan;
                cmd.Parameters.Add(p6);

                var p7 = cmd.CreateParameter();
                p7.ParameterName = "@diemTruc";
                p7.Value = req.DiemTruc;
                cmd.Parameters.Add(p7);

                var p8 = cmd.CreateParameter();
                p8.ParameterName = "@diemTrucCc";
                p8.Value = req.DiemTrucCc;
                cmd.Parameters.Add(p8);

                var p9 = cmd.CreateParameter();
                p9.ParameterName = "@diemLaymau";
                p9.Value = req.DiemLayMau;
                cmd.Parameters.Add(p9);

                var p10 = cmd.CreateParameter();
                p10.ParameterName = "@thangNam";
                p10.Value = req.ThangNam;
                cmd.Parameters.Add(p10);

                var p11 = cmd.CreateParameter();
                p11.ParameterName = "@ngayTao";
                p11.Value = DateOnly.FromDateTime(DateTime.Now);
                cmd.Parameters.Add(p11);

                int newId = Convert.ToInt16( await cmd.ExecuteScalarAsync());
                
                return Ok(new
                {
                    message = "Thêm mới điểm kế hoạch thành công.",
                    diemKeHoachId = newId,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
    public class DsDiemKeHoachRequest
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
        public int? OfficerType { get; set; }
        public int? DiemTrucCc { get; set; }
        public int? DiemLayMau { get; set; }
    }

    public class ThemDiemKeHoachRequest
    {
        
        [Required]
        public int KhoaId { get; set; }
        [Required]
        public int BacSiId { get; set; }
        [Required]
        public string BacSi { get; set; }
        public int? DiemKeHoach { get; set; }
        public int? SoBuoiTruc { get; set; }
        public int? SoBenhNhan { get; set; }
        public int? DiemTruc { get; set; }
        public int? DiemTrucCc { get; set; }
        public int? DiemLayMau { get; set; }
        [Required]
        public string ThangNam { get; set; }
    }
}