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
    public class Xml2Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public Xml2Controller(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }
        [Authorize]
        [HttpPost("get_ds_thuoc_by_malk")]
        public async Task<ActionResult<object>> GetDsThuocByMaLk([FromBody] GetDsThuocByMaLkRequest? req)
        {
            if (req == null)
            {
                return BadRequest("Yêu cầu không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(req.MaLk))
            {
                return BadRequest("Chưa chọn bệnh nhân");
            }

            try
            {
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

                var whereBuilder = new System.Text.StringBuilder(" WHERE MA_LK = @maLk");
                var paramList = new List<DbParameter>();

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                var maLkParam = tempCmd.CreateParameter();
                maLkParam.ParameterName = "@maLk";
                maLkParam.Value = req.MaLk.Trim();
                paramList.Add(maLkParam);

                if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
                {
                    var p1 = tempCmd.CreateParameter();
                    p1.ParameterName = "@search";
                    p1.Value = $"%{req.SearchTerm}%";
                    paramList.Add(p1);

                    whereBuilder.Append(" AND (MA_THUOC LIKE @search OR TEN_THUOC LIKE @search)");
                }

                var sql = $"SELECT * FROM `{dbData}`.xml2" + whereBuilder.ToString() + $" LIMIT {pageSize} OFFSET {offset}";

                var dsThuoc = await _context.xml2
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT CAST(COUNT(*) AS SIGNED) FROM `{dbData}`.xml2" + whereBuilder.ToString();

                    cmd.Parameters.Clear();
                    foreach (var p in paramList)
                    {
                        var np = cmd.CreateParameter();
                        np.ParameterName = p.ParameterName;
                        np.Value = p.Value;
                        cmd.Parameters.Add(np);
                    }

                    var scalar = await cmd.ExecuteScalarAsync();
                    totalRecords = int.TryParse(scalar?.ToString(), out int result) ? result : 0;
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    DsThuoc = dsThuoc
				});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
    public class GetDsThuocByMaLkRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? MaLk { get; set; }
        public string? SearchTerm { get; set; }
    }
}