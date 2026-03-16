using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoaiPhepController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhepController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "tao_phieu")]
        [HttpGet("GetAllLoaiPhep")]
        public async Task<IActionResult> GetAllLoaiPhep()
        {
            var loaiPhepList = await _context.loai_phep.ToListAsync();
            return Ok(loaiPhepList);
        }
        [Authorize(Roles = "admin")]
        [HttpGet("GetPagedLoaiPhep")]
        public async Task<IActionResult> GetPagedLoaiPhep(int currentPage = 1, int pageSize = 10, string searchTerm = "Nội dung tìm kiếm")
        {
            if (currentPage <= 0) currentPage = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.loai_phep.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm) && searchTerm != "Nội dung tìm kiếm")
            {
                query = query.Where(lp => lp.ten_loai_phep.Contains(searchTerm));
            }

            int totalRecords = await query.CountAsync();
            var pagedData = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageSize = pageSize,
                CurrentPage = currentPage,
                Data = pagedData
            });
        }
        [Authorize(Roles = "admin")]
        [HttpPost("CreateLoaiPhep")]
        public async Task<IActionResult> CreateLoaiPhep([FromBody] LoaiPhepDto loaiPhepDto)
        {
            if (loaiPhepDto == null || string.IsNullOrEmpty(loaiPhepDto.ten_loai_phep) || loaiPhepDto.so_ngay_nghi < 0)
            {
                return BadRequest("Thông tin tạo mới không hợp lệ.");
            }
            var loaiPhep = new LoaiPhep(loaiPhepDto.ten_loai_phep, loaiPhepDto.so_ngay_nghi);

            _context.loai_phep.Add(loaiPhep);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo loại nghỉ phép thành công", Data = loaiPhep });
        }
        [Authorize(Roles = "admin")]
        [HttpPut("UpdateLoaiPhep/{id}")]
        public async Task<IActionResult> UpdateLoaiPhep(ulong id, [FromBody] LoaiPhepDto loaiPhepDto)
        {
            if (loaiPhepDto == null || string.IsNullOrEmpty(loaiPhepDto.ten_loai_phep) || loaiPhepDto.so_ngay_nghi < 0)
            {
                return BadRequest("Thông tin cập nhật không hợp lệ.");
            }

            var existingLoaiPhep = await _context.loai_phep.FindAsync(id);
            if (existingLoaiPhep == null)
            {
                return NotFound("Không tìm thấy loại nghhỉ phép này.");
            }

            existingLoaiPhep.ten_loai_phep = loaiPhepDto.ten_loai_phep;
            existingLoaiPhep.so_ngay_nghi = loaiPhepDto.so_ngay_nghi;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật thành công.", Data = existingLoaiPhep });
        }
    }
}
