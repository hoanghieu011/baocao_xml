using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class ViTriController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ViTriController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetDsViTri(int pageNumber = 1, int pageSize = 10, string searchTerm = "Nội dung tìm kiếm")
        {
            var viTriQuery = _context.vi_tri
                .Where(bp => searchTerm == "Nội dung tìm kiếm" || string.IsNullOrEmpty(searchTerm) || bp.ma_vi_tri.Contains(searchTerm) || bp.ten_vi_tri.Contains(searchTerm) || bp.ten_vi_tri_en.Contains(searchTerm))
                .GroupBy(bp => new { bp.id, bp.ma_vi_tri, bp.ten_vi_tri, bp.ten_vi_tri_en })
                .Select(g => new ViTri
                {
                    id = g.Key.id,
                    ma_vi_tri = g.Key.ma_vi_tri,
                    ten_vi_tri = g.Key.ten_vi_tri,
                    ten_vi_tri_en = g.Key.ten_vi_tri_en
                });

            var totalItems = await viTriQuery.CountAsync();

            var viTris = await viTriQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = viTris
            };

            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateViTri(ulong id, [FromBody] UpdateViTriDto vi_tri)
        {
            var viTri = await _context.vi_tri.FindAsync(id);

            if (viTri == null)
            {
                return NotFound("Không tìm thấy vị trí tương ứng.");
            }
            if (string.IsNullOrEmpty(vi_tri.ten_vi_tri))
            {
                return BadRequest("Tên vị trí không được để trống.");
            }
            if (string.IsNullOrEmpty(vi_tri.ten_vi_tri_en))
            {
                return BadRequest("Tên vị trí không được để trống.");
            }

            viTri.ten_vi_tri = vi_tri.ten_vi_tri;
            viTri.ten_vi_tri_en = vi_tri.ten_vi_tri_en;

            _context.vi_tri.Update(viTri);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> CreateViTri([FromBody] ViTriDto viTri)
        {
            if (string.IsNullOrEmpty(viTri.ma_vi_tri))
            {
                return BadRequest("Mã vị trí không được để trống.");
            }
            if (string.IsNullOrEmpty(viTri.ten_vi_tri))
            {
                return BadRequest("Tên vị trí không được để trống.");
            }
            if (string.IsNullOrEmpty(viTri.ten_vi_tri_en))
            {
                return BadRequest("Tên vị trí không được để trống.");
            }
            var existingViTri = await _context.vi_tri
                    .FirstOrDefaultAsync(v => v.ma_vi_tri == viTri.ma_vi_tri);

            if (existingViTri != null)
            {
                return BadRequest("Mã vị trí đã tồn tại.");
            }

            var viTri_1 = new ViTri(viTri.ma_vi_tri, viTri.ten_vi_tri, viTri.ten_vi_tri_en);

            _context.vi_tri.Add(viTri_1);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetViTriById), new { id = viTri_1.id }, viTri_1);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetViTriById(ulong id)
        {
            var viTri = await _context.vi_tri.FindAsync(id);

            if (viTri == null)
            {
                return NotFound("Không tìm thấy vị trí.");
            }
            return Ok(viTri);
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAllViTri()
        {
            var DsViTri = await _context.vi_tri
                .ToListAsync();

            return Ok(DsViTri);
        }
    }
}