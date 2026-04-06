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
    public class LoaiDichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public LoaiDichVuController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Tra cứu danh sách loại dịch vụ.
        /// </summary>
        /// <returns>Trả về danh loại sách dịch vụ.</returns>
        [Authorize]
        [HttpGet("ds_loaidichvu")]
        public async Task<ActionResult<object>> GetDsDichVu()
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var sql = @"SELECT * FROM dmc_nhom_mabhyt" ;

                var dsLoaiDichVu = await _context.dmc_nhom_mabhyt
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new
                {
                    DsLoaiDichVu = dsLoaiDichVu,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
    }
}