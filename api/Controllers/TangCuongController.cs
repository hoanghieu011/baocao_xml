using API.Common;
using API.Controllers;
using API.Data;
using API.Models;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TangCuongController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DatabaseResolver _databaseResolver;

        public TangCuongController(ApplicationDbContext dbContext, DatabaseResolver databaseResolver)
        {
            _dbContext = dbContext;
            _databaseResolver = databaseResolver;
        }

        [Authorize]
        [HttpPost("ds_tangcuong")]
        public async Task<ActionResult<object>> GetDsTangCuong([FromBody] DsTangCuongRequest req)
        {
            try
            {

                // get list TangCuong by req.diemKeHoachId
                if (req == null || req.diemKeHoachId == 0) {
                    return BadRequest("diemKeHoachId là bắt buộc!");
                }
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _databaseResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                var sql = $"SELECT * FROM `{dbData}`.bc_tangcuong WHERE diemkehoachid = @diemkehoachId";
                var conn = _dbContext.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();
                var p = tempCmd.CreateParameter();
                p.ParameterName = "@diemkehoachId";
                p.Value = $"{req.diemKeHoachId}";

                var dsTangCuong = await _dbContext.tangcuong
                    .FromSqlRaw(sql, p)
                    .AsNoTracking()
                    .ToListAsync();
                return Ok(
                    new
                    {
                        dsTangCuong
                    });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi server: " + ex.Message);
            }
        }

        [Authorize(Roles = "EDIT_TANGCUONG, ADMIN")]
        [HttpPatch("capnhat_tangcuong")]
        public async Task<ActionResult<TangCuong>> CapNhatTangCuong([FromBody] CapNhatTangCuongRequest req)
        {
            try
            {
                if(req == null || req.dsTangCuong == null)
                {
                    return BadRequest("Không có dữ liệu cần cập nhật!");
                }
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _databaseResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var conn = _dbContext.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                // sql delete
                var sqlDel = $"DELETE FROM `{dbData}`.bc_tangcuong WHERE diemkehoachid = {req.diemKeHoachId}";
                // xóa bản ghi có sẵn
                cmd.CommandText = sqlDel;
                await cmd.ExecuteNonQueryAsync();


                if(req.dsTangCuong.Count > 0)
                {
                    // sql insert
                    var sb = new StringBuilder();
                    sb.Append($"INSERT INTO `{dbData}`.bc_tangcuong (diemkehoachid, khoaid, songay, diem ) VALUES ");
                    for (int i = 0; i < req.dsTangCuong.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append($"(@diemkehoachid{i}, @khoaid{i}, @songay{i}, @diem{i})");

                    }
                    // insert lại 
                    cmd.CommandText = sb.ToString();
                    for (int i = 0; i < req.dsTangCuong.Count; i++)
                    {
                        cmd.Parameters.Add(new MySqlParameter($"@diemkehoachid{i}", req.dsTangCuong[i].diemKeHoachId));
                        cmd.Parameters.Add(new MySqlParameter($"@khoaid{i}", req.dsTangCuong[i].khoaId));
                        cmd.Parameters.Add(new MySqlParameter($"@songay{i}", req.dsTangCuong[i].soNgay));
                        cmd.Parameters.Add(new MySqlParameter($"@diem{i}", req.dsTangCuong[i].diem));
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
                return Ok(new
                {
                    message = "Cập nhật thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi server: " + ex.Message);
            };
        }
        [Authorize(Roles ="DELETE_TANGCUONG, ADMIN")]
        [HttpDelete("xoa_tangcuong/{diemkehoachId}")]
        public async Task<ActionResult<TangCuong>> XoaTangCuong(int diemkehoachId)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("CSYTID")?.Value;
                // Lấy tên database động thông qua service dùng chung
                var dbData = await _databaseResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");
                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var conn = _dbContext.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"DELETE FROM `{dbData}`.bc_tangcuong WHERE diemkehoachid=@id";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@id";
                p1.Value = diemkehoachId;
                cmd.Parameters.Add(p1);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Xoá điểm tăng cường thành công.",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        
    }

    public class DsTangCuongRequest {
        [Required]
        public int diemKeHoachId { get; set; }
    }

    public class CapNhatTangCuongRequest
    {
        [Required]
        public List<TangCuong> dsTangCuong { get; set; }
        [Required]
        public int diemKeHoachId { get; set; }
    }
}
