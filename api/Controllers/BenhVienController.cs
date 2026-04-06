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
    public class BenhVienController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public BenhVienController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Tra cứu thông tin bệnh viện từ token.
        /// </summary>
        /// <returns>Trả về tên bệnh viện.</returns>
        [Authorize]
        [HttpGet("tt_benhvien")]
        public async Task<ActionResult<object>> GetDsDichVu()
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                var sql = $@"SELECT * FROM dmc_benhvien WHERE CSYTID = {csytid}" ;

                var data = await _context.dmc_benhvien
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new
                {
                    data = data,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}