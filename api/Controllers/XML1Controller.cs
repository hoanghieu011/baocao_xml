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
        [HttpGet("ds_benh_nhan")]
        public async Task<ActionResult<object>> GetDsBenhNhan(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                // Lấy tên database động thông qua service dùng chung
                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                // Validate identifier (chỉ cho phép chữ, số, underscore)
                if (!Regex.IsMatch(dbData, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Tên database không hợp lệ.");

                // normalize paging
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                // Build WHERE (parameterized for search term)
                string whereClause = string.Empty;
                object[]? sqlParams = null;
                if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "All")
                {
                    whereClause = "WHERE HOTEN LIKE {0}";
                    sqlParams = new object[] { $"%{searchTerm.Trim()}%" };
                }

                // Compose SELECT SQL (dbData validated, safe to interpolate as identifier)
                var sqlSelect = $"SELECT * FROM `{dbData}`.xml1 {whereClause} LIMIT {pageSize} OFFSET {offset}";

                List<XML1> dsBenhNhan;
                if (sqlParams is null)
                    dsBenhNhan = await _context.xml1
                        .FromSqlRaw(sqlSelect)
                        .AsNoTracking()
                        .ToListAsync();
                else
                    dsBenhNhan = await _context.xml1
                        .FromSqlRaw(sqlSelect, sqlParams)
                        .AsNoTracking()
                        .ToListAsync();

                // Lấy tổng số bản ghi (dùng same connection của DbContext)
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(*) FROM `{dbData}`.xml1 " + (whereClause == "" ? "" : whereClause.Replace("{0}", "@p"));
                    if (sqlParams is not null)
                    {
                        var p = cmd.CreateParameter();
                        p.ParameterName = "@p";
                        p.Value = sqlParams[0];
                        cmd.Parameters.Add(p);
                    }

                    var cnt = await cmd.ExecuteScalarAsync();
                    totalRecords = Convert.ToInt32(cnt ?? 0);
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
                // Tùy môi trường, bạn có thể log ex.ToString() bằng logger
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}