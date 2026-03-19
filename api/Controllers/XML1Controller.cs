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
    public class Xml1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public Xml1Controller(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }
        [Authorize]
        [HttpPost("ds_benh_nhan")]
        public async Task<ActionResult<object>> GetDsBenhNhan([FromBody] DsBenhNhanRequest req)
        {
            if (req.TuNgay == null || req.DenNgay == null)
            {
                return BadRequest("Từ ngày và đến ngày không được để trống");
            }
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                // Lấy database
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var pageNumber = Math.Max(1, req.PageNumber);
                var pageSize = Math.Clamp(req.PageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var whereBuilder = new System.Text.StringBuilder(" WHERE 1=1");
                var paramList = new List<DbParameter>();

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
                {
                    var p = tempCmd.CreateParameter();
                    p.ParameterName = "@search";
                    p.Value = $"%{req.SearchTerm}%";
                    paramList.Add(p);

                    whereBuilder.Append(" AND (HO_TEN LIKE @search OR MA_BN LIKE @search)");
                }

                if (req.TuNgay.HasValue && req.DenNgay.HasValue)
                {
                    var p1 = tempCmd.CreateParameter();
                    p1.ParameterName = "@tungay";
                    p1.Value = req.TuNgay.Value.Date;
                    paramList.Add(p1);

                    var p2 = tempCmd.CreateParameter();
                    p2.ParameterName = "@dengay";
                    p2.Value = req.DenNgay.Value.Date;
                    paramList.Add(p2);

                    whereBuilder.Append(" AND NGAY_RA BETWEEN @tungay AND @dengay");
                }

                var sql = $"SELECT * FROM `{dbData}`.xml1" + whereBuilder.ToString() + $" LIMIT {pageSize} OFFSET {offset}";

                var dsBenhNhan = await _context.xml1
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(*) FROM `{dbData}`.xml1" + whereBuilder.ToString();

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
                    DsBenhNhan = dsBenhNhan
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
    public class DsBenhNhanRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
    }
}