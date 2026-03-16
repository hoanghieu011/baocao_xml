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
    public class BoPhanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BoPhanController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetBoPhans(int pageNumber = 1, int pageSize = 10, string searchTerm = "Nội dung tìm kiếm")
        {
            var boPhanQuery = _context.bo_phan
                .Where(bp => searchTerm == "Nội dung tìm kiếm" || string.IsNullOrEmpty(searchTerm) || bp.ten_bo_phan.Contains(searchTerm))
                .GroupBy(bp => new { bp.id, bp.ten_bo_phan })
                .Select(g => new BoPhanDto
                {
                    id = g.Key.id,
                    ten_bo_phan = g.Key.ten_bo_phan,
                    so_luong_nhan_vien = _context.nhan_vien.Count(nv => nv.bo_phan_id == g.Key.id)
                });

            var totalItems = await boPhanQuery.CountAsync();

            var boPhans = await boPhanQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = boPhans
            };

            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBoPhan(ulong id, [FromBody] UpdateBoPhanDto bo_phan)
        {
            var boPhan = await _context.bo_phan.FindAsync(id);

            if (boPhan == null)
            {
                return NotFound("Không tìm thấy bộ phận.");
            }

            if (string.IsNullOrEmpty(bo_phan.ten_bo_phan))
            {
                return BadRequest("Tên bộ phận không được để trống.");
            }

            boPhan.ten_bo_phan = bo_phan.ten_bo_phan;

            _context.bo_phan.Update(boPhan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> CreateBoPhan([FromBody] UpdateBoPhanDto bo_phan)
        {
            if (string.IsNullOrEmpty(bo_phan.ten_bo_phan))
            {
                return BadRequest("Tên bộ phận không được để trống.");
            }

            var boPhan = new BoPhan(bo_phan.ten_bo_phan);

            _context.bo_phan.Add(boPhan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBoPhanById), new { id = boPhan.id }, boPhan);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBoPhanById(ulong id)
        {
            var boPhan = await _context.bo_phan.FindAsync(id);

            if (boPhan == null)
            {
                return NotFound("Không tìm thấy bộ phận.");
            }

            var boPhanDto = new BoPhanDto
            {
                id = boPhan.id,
                ten_bo_phan = boPhan.ten_bo_phan
            };

            return Ok(boPhanDto);
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAllBoPhans()
        {
            var boPhans = await _context.bo_phan
                .ToListAsync();

            return Ok(boPhans);
        }
    }
}
