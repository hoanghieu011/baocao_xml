using API.Common;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DatabaseResolver _dbResolver;
        public OrganizationController(ApplicationDbContext dbContext, DatabaseResolver dbResolver)
        {
            _dbContext = dbContext;
            _dbResolver = dbResolver;
        }

        [Authorize]
        [HttpGet("ds_organization")]
        public async Task<ActionResult<object>> GetDsOfficer(string? term)
        {
            try
            {
                var csytid = User.FindFirst("CSYTID")?.Value;
                if (string.IsNullOrEmpty(csytid))
                    return Unauthorized();

                var hasSearch = !string.IsNullOrWhiteSpace(term) && term != "All";

                var sql = @"
                    SELECT * 
                    FROM ORG_ORGANIZATION
                    WHERE CSYTID = @csytid
                    AND STATUS = 1
                    AND MA_KHOA IS NOT NULL
                ";

                var parameters = new List<object>
                {
                    new MySqlConnector.MySqlParameter("@csytid", csytid)
                };

                if (hasSearch)
                {
                    sql += " AND (MA_KHOA LIKE @term OR ORG_NAME LIKE @term)";
                    parameters.Add(new MySqlConnector.MySqlParameter("@term", $"%{term}%"));
                }

                var ds_organization = await _dbContext.org_organization
                    .FromSqlRaw(sql, parameters.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new { ds_organization });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}
