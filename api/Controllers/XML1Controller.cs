using API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Xml1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        // private readonly DatabaseResolver _dbResolver;

        public Xml1Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("ds_benh_nhan")]
        public async Task<ActionResult<object>> GetDsBenhNhan(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var conn = _context.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                string dbData;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DB_DATA FROM dmc_benhvien 
                        WHERE CSYTID = (
                            SELECT CSYTID FROM org_officer 
                            WHERE OFFICER_ID = (
                                SELECT OFFICER_ID FROM adm_user 
                                WHERE USER_NAME = @user
                            )
                        )";

                    var param = cmd.CreateParameter();
                    param.ParameterName = "@user";
                    param.Value = userName;
                    cmd.Parameters.Add(param);

                    var result = await cmd.ExecuteScalarAsync();
                    dbData = result?.ToString();
                }

                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var sqlSelect = $"SELECT * FROM `{dbData}`.xml1 LIMIT {pageSize} OFFSET {offset}";

                var dsBenhNhan = await _context.xml1
                    .FromSqlRaw(sqlSelect)
                    .AsNoTracking()
                    .ToListAsync();

                int totalRecords = 0;
                using (var cmd2 = conn.CreateCommand())
                {
                    cmd2.CommandText = $"SELECT COUNT(*) FROM `{dbData}`.xml1";
                    var cnt = await cmd2.ExecuteScalarAsync();
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
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}