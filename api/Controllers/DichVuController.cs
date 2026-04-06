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
    public class DichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public DichVuController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Tra cứu danh sách dịch vụ.
        /// </summary>
        /// <returns>Trả về danh sách dịch vụ.</returns>
        [Authorize]
        [HttpPost("ds_dichvu")]
        public async Task<ActionResult<object>> GetDsDichVu([FromBody] DsDichVuRequest req)
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

                var whereBuilder = new System.Text.StringBuilder(" WHERE 1=1");
                var paramList = new List<DbParameter>();

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                whereBuilder.Append($" AND a.CSYTID = {csytid} and a.TRANGTHAI = 1 and a.NHOM_MABHYT_ID = b.NHOM_MABHYT_ID");

                if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
                {
                    var p = tempCmd.CreateParameter();
                    p.ParameterName = "@search";
                    p.Value = $"%{req.SearchTerm}%";
                    paramList.Add(p);

                    whereBuilder.Append(" AND (a.MA_DICHVU LIKE @search OR a.TEN_DICHVU LIKE @search)");
                }

                if (req.IdLoaiDV != null && req.IdLoaiDV != 0)
                {
                    whereBuilder.Append($" AND (a.NHOM_MABHYT_ID = {req.IdLoaiDV})");
                }

                var sql = @"SELECT 
                                a.MA_DICHVU,
                                a.DICHVUID, a.TEN_DICHVU, b.TENNHOM, a.DONVI, a.CSYTID, a.GIA_BHYT, a.CHIPHI, a.HESO, a.nhom_mabhyt_id
                                FROM dmc_dichvu a, dmc_nhom_mabhyt b"
                            + whereBuilder.ToString() + $" ORDER BY a.LOAIID, a.nhom_mabhyt_id LIMIT {pageSize} OFFSET {offset}" ;

                var dsDichVu = await _context.dto_dichvu
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT 
                                count(1)
                                FROM dmc_dichvu a, dmc_nhom_mabhyt b"
                            + whereBuilder.ToString();

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
        /// Cập nhật chi phí, hệ số dịch vụ.
        /// </summary>
        /// <returns>Trả về kq cập nhật.</returns>
        [Authorize]
        [HttpPut("cap-nhat-dichvu")]
        public async Task<ActionResult<object>> CapNhatDichVu([FromBody] CapNhatDichVuRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                if (req.DichVuId <= 0)
                    return BadRequest("DichVuId không hợp lệ.");

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
                cmd.CommandText = @"
                    UPDATE dmc_dichvu
                    SET CHIPHI = @chiphi,
                        HESO = @heso
                    WHERE DICHVUID = @dichvuid
                        AND CSYTID = @csytid";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@chiphi";
                p1.Value = req.ChiPhi;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@heso";
                p2.Value = req.HeSo;
                cmd.Parameters.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.ParameterName = "@dichvuid";
                p3.Value = req.DichVuId;
                cmd.Parameters.Add(p3);

                var p4 = cmd.CreateParameter();
                p4.ParameterName = "@csytid";
                p4.Value = csytid;
                cmd.Parameters.Add(p4);

                var affectedRows = await cmd.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                    return NotFound(new { message = "Không tìm thấy dịch vụ cần cập nhật." });

                return Ok(new
                {
                    message = "Cập nhật dịch vụ thành công.",
                    affectedRows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
    public class DsDichVuRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public int? IdLoaiDV { get; set; }
    }

    public class CapNhatDichVuRequest
    {
        public int DichVuId { get; set; }
        public decimal? ChiPhi { get; set; }
        public decimal? HeSo { get; set; }
    }
}