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
    public class OfficerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public OfficerController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Tra cứu danh sách Officer.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ds_officer")]
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
                    FROM ORG_OFFICER 
                    WHERE CSYTID = @csytid
                    AND STATUS = 1 
                    AND MA_BAC_SI IS NOT NULL
                ";

                var parameters = new List<object>
                {
                    new MySqlConnector.MySqlParameter("@csytid", csytid)
                };

                if (hasSearch)
                {
                    sql += " AND (MA_BAC_SI LIKE @term OR OFFICER_NAME LIKE @term)";
                    parameters.Add(new MySqlConnector.MySqlParameter("@term", $"%{term}%"));
                }

                var ds_officer = await _context.org_officer
                    .FromSqlRaw(sql, parameters.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new { ds_officer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}