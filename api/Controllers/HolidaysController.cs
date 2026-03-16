using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidaysController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HolidaysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Holidays?year=2023&page=1&pageSize=10
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetHolidays([FromQuery] int year, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page và pageSize phải lớn hơn 0.");
            }

            // Lọc dữ liệu theo năm và sắp xếp theo ngày nghỉ tăng dần
            var query = _context.holiday.Where(h => h.year == year);
            var totalCount = await query.CountAsync();

            var holidays = await query
                                .OrderBy(h => h.ngay_nghi)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            var result = new
            {
                totalCount,
                data = holidays
            };

            return Ok(result);
        }
        [Authorize(Roles = "admin")]
        [HttpPatch("{id}/description")]
        public async Task<IActionResult> UpdateHolidayDescription(ulong id, [FromBody] UpdateDescriptionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.mo_ta))
            {
                return BadRequest("Dữ liệu không hợp lệ. Vui lòng cung cấp mo_ta.");
            }

            var holiday = await _context.holiday.FindAsync(id);
            if (holiday == null)
            {
                return NotFound("Không tìm thấy bản ghi với id đã cho.");
            }

            holiday.mo_ta = request.mo_ta;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công." });
        }
        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllHolidaysByYear([FromQuery] int year)
        {
            var holidays = await _context.holiday
                                         .Where(h => h.year == year)
                                         .OrderBy(h => h.ngay_nghi)
                                         .ToListAsync();

            var result = new
            {
                data = holidays
            };

            return Ok(result);
        }
        [Authorize(Roles = "admin")]
        [HttpGet("years")]
        public async Task<IActionResult> GetDistinctYears()
        {
            var years = await _context.holiday
                .Select(h => h.year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

            return Ok(new { data = years });
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoliday(ulong id)
        {
            var holiday = await _context.holiday.FindAsync(id);
            if (holiday == null)
            {
                return NotFound("Không tìm thấy bản ghi với id đã cho.");
            }

            _context.holiday.Remove(holiday);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> CreateHoliday([FromBody] Holiday holiday)
        {
            if (holiday == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            bool exists = await _context.holiday
                                .AnyAsync(h => h.ngay_nghi.Date == holiday.ngay_nghi.Date);
            if (exists)
            {
                return BadRequest(new { code = 1, message = "Ngày nghỉ đã tồn tại." });
            }

            _context.holiday.Add(holiday);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHolidays), new { year = holiday.year }, holiday);
        }
    }
    public class UpdateDescriptionRequest
    {
        public string mo_ta { get; set; }
    }
}
