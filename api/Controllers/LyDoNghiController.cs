using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LyDoNghiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LyDoNghiController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetDsLyDo(int pageNumber = 1, int pageSize = 10, string searchTerm = "Nội dung tìm kiếm")
        {
            var lyDoQuery = _context.ly_do_nghi
                .Where(bp => searchTerm == "Nội dung tìm kiếm" || string.IsNullOrEmpty(searchTerm) || bp.ky_hieu.Contains(searchTerm) || bp.dien_giai.Contains(searchTerm))
                .GroupBy(bp => new { bp.id, bp.ky_hieu, bp.dien_giai })
                .Select(g => new LyDoNghi
                {
                    id = g.Key.id,
                    ky_hieu = g.Key.ky_hieu,
                    dien_giai = g.Key.dien_giai,
                });

            var totalItems = await lyDoQuery.CountAsync();

            var dsLyDo = await lyDoQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = dsLyDo
            };

            return Ok(result);
        }
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLyDoNghi(ulong id, [FromBody] LyDoNghiDto ly_do)
        {
            var lyDo = await _context.ly_do_nghi.FindAsync(id);

            if (lyDo == null)
            {
                return NotFound("Không tìm thấy lý do nghỉ.");
            }
            if (string.IsNullOrEmpty(ly_do.ky_hieu))
            {
                return BadRequest("Ký hiệu không được để trống.");
            }
            if (string.IsNullOrEmpty(ly_do.dien_giai))
            {
                return BadRequest("Lý do nghỉ không được để trống.");
            }
            if (!string.Equals(lyDo.ky_hieu, ly_do.ky_hieu, StringComparison.OrdinalIgnoreCase))
            {
                var isDuplicate = await _context.ly_do_nghi.AnyAsync(ld => ld.ky_hieu == ly_do.ky_hieu);
                if (isDuplicate)
                {
                    return BadRequest("Ký hiệu đã tồn tại.");
                }
            }

            lyDo.ky_hieu = ly_do.ky_hieu;
            lyDo.dien_giai = ly_do.dien_giai;

            _context.ly_do_nghi.Update(lyDo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> CreateLyDoNghi([FromBody] LyDoNghiDto ly_do)
        {
            if (string.IsNullOrEmpty(ly_do.ky_hieu))
            {
                return BadRequest("Ký hiệu lý do nghỉ không được để trống.");
            }
            if (string.IsNullOrEmpty(ly_do.dien_giai))
            {
                return BadRequest("Diễn giải của lý do nghỉ không được để trống.");
            }
            var existingLyDo = await _context.ly_do_nghi
                    .FirstOrDefaultAsync(v => v.ky_hieu == ly_do.ky_hieu);

            if (existingLyDo != null)
            {
                return BadRequest("Ký hiệu của lý do nghỉ đã tồn tại.");
            }

            var lyDo = new LyDoNghi(ly_do.ky_hieu, ly_do.dien_giai);

            _context.ly_do_nghi.Add(lyDo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLyDoNghiById), new { id = lyDo.id }, lyDo);
        }
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLyDoNghiById(ulong id)
        {
            var lyDo = await _context.ly_do_nghi.FindAsync(id);

            if (lyDo == null)
            {
                return NotFound("Không tìm thấy lý do nghỉ.");
            }
            return Ok(lyDo);
        }
        [Authorize(Roles = "tao_phieu")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllLyDo()
        {
            var DsLyDo = await _context.ly_do_nghi
                .ToListAsync();

            return Ok(DsLyDo);
        }
    }
}