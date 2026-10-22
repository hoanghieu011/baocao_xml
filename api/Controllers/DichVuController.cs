using API.Common;
using API.Data;
using API.DTO;
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
    public class DichVuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;
        private readonly ILogger<DichVuController> _logger;

        public DichVuController(ApplicationDbContext context, DatabaseResolver dbResolver, ILogger<DichVuController> logger)
        {
            _context = context;
            _dbResolver = dbResolver;
            _logger = logger;
        }

        /// <summary>
        /// Tra cứu danh sách dịch vụ.
        /// </summary>
        /// <returns>Trả về danh sách dịch vụ.</returns>
        [Authorize]
        [HttpPost("ds_dichvu")]
        public async Task<ActionResult<object>> GetDsDichVu([FromBody] DsDichVuRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                // Danh mục dịch vụ nằm trong DB dữ liệu của bệnh viện (his_data_<bv>.dmc_dichvu)
                var dbName = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbName) || !Regex.IsMatch(dbName, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Không xác định được cơ sở dữ liệu của người dùng.");

                var pageNumber = Math.Max(1, req.PageNumber);
                var pageSize = Math.Clamp(req.PageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                // Xây dựng điều kiện lọc dùng chung cho cả tra cứu và xuất Excel
                var whereBuilder = BuildDichVuFilter(req, csytid, tempCmd, out var paramList);

                var sql = $@"SELECT
                                a.MA_DICHVU,
                                a.DICHVUID, a.TEN_DICHVU, b.TENNHOM, a.DONVI, a.CSYTID, a.GIA_BHYT, a.CHIPHI, a.HESO, a.nhom_mabhyt_id, a.HESO_CLS_BS, a.HESO_CLS_DD
                                FROM `{dbName}`.dmc_dichvu a, dmc_nhom_mabhyt b"
                            + whereBuilder.ToString() + $" ORDER BY a.LOAIID, a.nhom_mabhyt_id LIMIT {pageSize} OFFSET {offset}" ;

                var dsDichVu = await _context.dto_dichvu
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"SELECT
                                count(1)
                                FROM `{dbName}`.dmc_dichvu a, dmc_nhom_mabhyt b"
                            + whereBuilder.ToString();

                    cmd.Parameters.Clear();
                    foreach (var p in paramList)
                    {
                        var np = cmd.CreateParameter();
                        np.ParameterName = p.ParameterName;
                        np.Value = p.Value;
                        cmd.Parameters.Add(np);
                    }

                    var scalar = await cmd.ExecuteScalarAsync();
                    totalRecords = Convert.ToInt32(scalar ?? 0);
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    DsDichVu = dsDichVu,
                    id = req.IdLoaiDV
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xuất danh sách dịch vụ ra file Excel (.xlsx) theo đúng bộ lọc hiện tại của màn hình.
        /// Không phân trang — xuất toàn bộ bản ghi khớp bộ lọc trong phạm vi bệnh viện của user.
        /// </summary>
        [Authorize]
        [HttpPost("ds_dichvu_excel")]
        public async Task<IActionResult> ExportDsDichVu([FromBody] DsDichVuRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                // Danh mục dịch vụ nằm trong DB dữ liệu của bệnh viện (his_data_<bv>.dmc_dichvu)
                var dbName = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbName) || !Regex.IsMatch(dbName, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Không xác định được cơ sở dữ liệu của người dùng.");

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                // Dùng chung điều kiện lọc với tra cứu => dữ liệu xuất khớp màn hình
                var whereBuilder = BuildDichVuFilter(req, csytid, tempCmd, out var paramList);

                // Không LIMIT/OFFSET: lấy toàn bộ bản ghi khớp bộ lọc
                var sql = $@"SELECT
                                a.MA_DICHVU,
                                a.DICHVUID, a.TEN_DICHVU, b.TENNHOM, a.DONVI, a.CSYTID, a.GIA_BHYT, a.CHIPHI, a.HESO, a.nhom_mabhyt_id, a.HESO_CLS_BS, a.HESO_CLS_DD
                                FROM `{dbName}`.dmc_dichvu a, dmc_nhom_mabhyt b"
                            + whereBuilder.ToString() + " ORDER BY a.LOAIID, a.nhom_mabhyt_id";

                var dsDichVu = await _context.dto_dichvu
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                var columns = new List<ExcelColumn<DichVuDto>>
                {
                    new() { Header = "Mã dịch vụ / vật tư", Value = x => x.ma_dichvu, AsText = true, Width = 22 },
                    new() { Header = "Tên dịch vụ / vật tư", Value = x => x.ten_dichvu, Width = 45 },
                    new() { Header = "Đơn vị", Value = x => x.donvi, Width = 10 },
                    new() { Header = "Giá", Value = x => x.gia_bhyt, NumberFormat = "#,##0", Width = 14 },
                    new() { Header = "Chi phí", Value = x => x.chiphi, NumberFormat = "#,##0.##", Width = 14 },
                    new() { Header = "Hệ số", Value = x => x.heso, NumberFormat = "#,##0.##", Width = 10 },
                    new() { Header = "Hệ số CLS BS", Value = x => x.heso_cls_bs, NumberFormat = "#,##0.##", Width = 12 },
                    new() { Header = "Hệ số CLS ĐD", Value = x => x.heso_cls_dd, NumberFormat = "#,##0.##", Width = 12 },
                    new() { Header = "Loại dịch vụ", Value = x => x.tennhom, Width = 30 },
                };

                var bytes = ExcelExportHelper.Build(dsDichVu, columns, "DANH_MUC_DICH_VU");
                var fileName = $"DanhMucDichVu_{DateTime.Now:yyyyMMddHHmm}.xlsx";

                return File(bytes, ExcelExportHelper.XlsxContentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xây dựng mệnh đề WHERE + tham số dùng chung cho tra cứu và xuất Excel danh mục dịch vụ.
        /// Luôn gắn điều kiện CSYTID (phạm vi bệnh viện của user) để đảm bảo phân quyền dữ liệu.
        /// </summary>
        private static System.Text.StringBuilder BuildDichVuFilter(
            DsDichVuRequest req, string? csytid, DbCommand tempCmd, out List<DbParameter> paramList)
        {
            var whereBuilder = new System.Text.StringBuilder(" WHERE 1=1");
            paramList = new List<DbParameter>();

            whereBuilder.Append($" AND a.CSYTID = {csytid} and a.TRANGTHAI = 1 and a.NHOM_MABHYT_ID = b.NHOM_MABHYT_ID");

            if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
            {
                var p = tempCmd.CreateParameter();
                p.ParameterName = "@search";
                p.Value = $"%{req.SearchTerm}%";
                paramList.Add(p);

                whereBuilder.Append(" AND (a.MA_DICHVU LIKE @search OR a.TEN_DICHVU LIKE @search)");
            }

            if (req.IdLoaiDV != null && req.IdLoaiDV != 0)
            {
                whereBuilder.Append($" AND (a.NHOM_MABHYT_ID = {req.IdLoaiDV})");
            }

            return whereBuilder;
        }

        /// <summary>
        /// Cập nhật chi phí, hệ số dịch vụ.
        /// </summary>
        /// <returns>Trả về kq cập nhật.</returns>
        [Authorize(Roles ="EDIT_DICHVU, ADMIN")]
        [HttpPut("cap-nhat-dichvu")]
        public async Task<ActionResult<object>> CapNhatDichVu([FromBody] CapNhatDichVuRequest req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                if (req.DichVuId <= 0)
                    return BadRequest("DichVuId không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                var csytid = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("CSYTID")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                // Danh mục dịch vụ nằm trong DB dữ liệu của bệnh viện (his_data_<bv>.dmc_dichvu)
                var dbName = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbName) || !Regex.IsMatch(dbName, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Không xác định được cơ sở dữ liệu của người dùng.");

                var conn = _context.Database.GetDbConnection();

                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    UPDATE `{dbName}`.dmc_dichvu
                    SET CHIPHI = @chiphi,
                        HESO = @heso,
                        HESO_CLS_BS = @heso_cls_bs,
                        HESO_CLS_DD = @heso_cls_dd
                    WHERE DICHVUID = @dichvuid
                        AND CSYTID = @csytid";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@chiphi";
                p1.Value = req.ChiPhi;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@heso";
                p2.Value = req.HeSo;
                cmd.Parameters.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.ParameterName = "@dichvuid";
                p3.Value = req.DichVuId;
                cmd.Parameters.Add(p3);

                var p4 = cmd.CreateParameter();
                p4.ParameterName = "@csytid";
                p4.Value = csytid;
                cmd.Parameters.Add(p4);

                var p5 = cmd.CreateParameter();
                p5.ParameterName = "@heso_cls_bs";
                p5.Value = req.HeSo_Cls_bs;
                cmd.Parameters.Add(p5);

                var p6 = cmd.CreateParameter();
                p6.ParameterName = "@heso_cls_dd";
                p6.Value = req.HeSo_Cls_dd;
                cmd.Parameters.Add(p6);

                var affectedRows = await cmd.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                    return NotFound(new { message = "Không tìm thấy dịch vụ cần cập nhật." });

                return Ok(new
                {
                    message = "Cập nhật dịch vụ thành công.",
                    affectedRows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }
        /// <summary>
        /// Subquery lấy giá BHYT mới nhất (DON_GIA_BH) theo từng khóa dịch vụ trong xml3.
        /// Khóa ánh xạ = (k_ma, k_ten) chuẩn hóa giống BaoCaoController:
        /// k_ma = MA_DICH_VU, rỗng thì lấy MA_VAT_TU; k_ten = TEN_DICH_VU, rỗng thì lấy TEN_VAT_TU.
        /// Chỉ lấy bản ghi có DON_GIA_BH > 0 và chọn bản mới nhất theo NGAY_YL DESC, XML3ID DESC.
        /// {0} = tên database dữ liệu bệnh viện (đã validate regex trước khi nội suy).
        /// </summary>
        private const string LatestGiaSrcSubquery = @"
            SELECT k_ma, k_ten, DON_GIA_BH AS gia_moi, NGAY_YL AS ngay_yl FROM (
                SELECT IF(IFNULL(b.MA_DICH_VU,'')<>'', b.MA_DICH_VU, b.MA_VAT_TU)   AS k_ma,
                       IF(IFNULL(b.TEN_DICH_VU,'')<>'', b.TEN_DICH_VU, b.TEN_VAT_TU) AS k_ten,
                       b.DON_GIA_BH, b.NGAY_YL,
                       ROW_NUMBER() OVER (
                         PARTITION BY IF(IFNULL(b.MA_DICH_VU,'')<>'', b.MA_DICH_VU, b.MA_VAT_TU),
                                      IF(IFNULL(b.TEN_DICH_VU,'')<>'', b.TEN_DICH_VU, b.TEN_VAT_TU)
                         ORDER BY b.NGAY_YL DESC, b.XML3ID DESC) AS rn
                FROM `{0}`.xml3 b
                WHERE b.DON_GIA_BH IS NOT NULL AND b.DON_GIA_BH > 0
            ) t WHERE t.rn = 1";

        /// <summary>
        /// Xem trước (preview) việc cập nhật giá dịch vụ từ dữ liệu xml3.
        /// Không ghi dữ liệu. Trả thống kê + danh sách so sánh giá hiện tại và giá mới nhất.
        /// </summary>
        [Authorize(Roles = "EDIT_DICHVU, ADMIN")]
        [HttpPost("preview-cap-nhat-gia")]
        public async Task<ActionResult<object>> PreviewCapNhatGia()
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var dbName = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbName) || !Regex.IsMatch(dbName, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Không xác định được cơ sở dữ liệu của người dùng.");

                var csytid = await _dbResolver.GetCsytIdByUserAsync(userName);
                if (string.IsNullOrEmpty(csytid))
                    return BadRequest("Không xác định được cơ sở y tế của người dùng.");

                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                var danhSach = new List<PreviewGiaItem>();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        SELECT dv.DICHVUID, dv.MA_DICHVU, dv.TEN_DICHVU, dv.GIA_BHYT AS gia_hien_tai,
                               src.gia_moi, src.ngay_yl
                        FROM `{dbName}`.dmc_dichvu dv
                        LEFT JOIN ( {string.Format(LatestGiaSrcSubquery, dbName)} ) src
                               ON src.k_ma = dv.MA_DICHVU AND src.k_ten = dv.TEN_DICHVU
                        WHERE dv.CSYTID = @csytid AND dv.TRANGTHAI = 1
                        ORDER BY dv.MA_DICHVU";

                    var p = cmd.CreateParameter();
                    p.ParameterName = "@csytid";
                    p.Value = csytid;
                    cmd.Parameters.Add(p);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var dichvuid = reader.GetInt32(0);
                        var maDichVu = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var tenDichVu = reader.IsDBNull(2) ? null : reader.GetString(2);
                        int? giaHienTai = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                        decimal? giaMoiRaw = reader.IsDBNull(4) ? null : reader.GetDecimal(4);
                        DateTime? ngayYl = reader.IsDBNull(5) ? null : reader.GetDateTime(5);

                        string trangThai;
                        long? giaMoi = null;
                        if (giaMoiRaw == null)
                        {
                            trangThai = "KHONG_CO_NGUON";
                        }
                        else
                        {
                            giaMoi = (long)Math.Round(giaMoiRaw.Value, MidpointRounding.AwayFromZero);
                            trangThai = giaMoi == (giaHienTai ?? 0) ? "KHONG_DOI" : "THAY_DOI";
                        }

                        danhSach.Add(new PreviewGiaItem
                        {
                            dichvuid = dichvuid,
                            maDichVu = maDichVu,
                            tenDichVu = tenDichVu,
                            giaHienTai = giaHienTai,
                            giaMoi = giaMoi,
                            ngayYl = ngayYl,
                            trangThai = trangThai
                        });
                    }
                }

                var resp = new PreviewGiaResponse
                {
                    tongSoDichVu = danhSach.Count,
                    soCoNguon = danhSach.Count(x => x.trangThai != "KHONG_CO_NGUON"),
                    soGiaThayDoi = danhSach.Count(x => x.trangThai == "THAY_DOI"),
                    soGiaKhongDoi = danhSach.Count(x => x.trangThai == "KHONG_DOI"),
                    soKhongCoNguon = danhSach.Count(x => x.trangThai == "KHONG_CO_NGUON"),
                    danhSach = danhSach
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận cập nhật giá dịch vụ (GIA_BHYT) từ giá BHYT mới nhất trong xml3.
        /// Chỉ cập nhật các DICHVUID người dùng gửi lên, tính lại giá mới nhất ngay trong SQL
        /// (re-check tránh dữ liệu đổi giữa preview và confirm). Toàn bộ chạy trong transaction.
        /// </summary>
        [Authorize(Roles = "EDIT_DICHVU, ADMIN")]
        [HttpPost("confirm-cap-nhat-gia")]
        public async Task<ActionResult<object>> ConfirmCapNhatGia([FromBody] ConfirmCapNhatGiaRequest req)
        {
            try
            {
                if (req?.DichVuIds == null || req.DichVuIds.Count == 0)
                    return BadRequest("Không có dịch vụ nào được chọn để cập nhật.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var dbName = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbName) || !Regex.IsMatch(dbName, @"^[A-Za-z0-9_]+$"))
                    return BadRequest("Không xác định được cơ sở dữ liệu của người dùng.");

                var csytid = await _dbResolver.GetCsytIdByUserAsync(userName);
                if (string.IsNullOrEmpty(csytid))
                    return BadRequest("Không xác định được cơ sở y tế của người dùng.");

                // Chỉ nhận các id hợp lệ, loại trùng
                var ids = req.DichVuIds.Where(x => x > 0).Distinct().ToList();
                if (ids.Count == 0)
                    return BadRequest("Danh sách dịch vụ không hợp lệ.");

                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var tran = await conn.BeginTransactionAsync();
                try
                {
                    int affectedRows;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;

                        var idParams = ids.Select((_, i) => $"@id{i}").ToList();
                        cmd.CommandText = $@"
                            UPDATE `{dbName}`.dmc_dichvu dv
                            JOIN ( {string.Format(LatestGiaSrcSubquery, dbName)} ) src
                                 ON src.k_ma = dv.MA_DICHVU AND src.k_ten = dv.TEN_DICHVU
                            SET dv.GIA_BHYT = ROUND(src.gia_moi)
                            WHERE dv.CSYTID = @csytid AND dv.TRANGTHAI = 1
                              AND dv.DICHVUID IN ({string.Join(",", idParams)})
                              AND ROUND(src.gia_moi) <> dv.GIA_BHYT";

                        var pc = cmd.CreateParameter();
                        pc.ParameterName = "@csytid";
                        pc.Value = csytid;
                        cmd.Parameters.Add(pc);

                        for (int i = 0; i < ids.Count; i++)
                        {
                            var pi = cmd.CreateParameter();
                            pi.ParameterName = $"@id{i}";
                            pi.Value = ids[i];
                            cmd.Parameters.Add(pi);
                        }

                        affectedRows = await cmd.ExecuteNonQueryAsync();
                    }

                    await tran.CommitAsync();

                    _logger.LogInformation(
                        "Cập nhật giá dịch vụ từ xml3: user={User}, csytid={Csytid}, soGui={SoGui}, soCapNhat={SoCapNhat}",
                        userName, csytid, ids.Count, affectedRows);

                    return Ok(new ConfirmGiaResponse
                    {
                        thanhCong = true,
                        soDichVuCapNhat = affectedRows,
                        soBoQua = ids.Count - affectedRows,
                        message = $"Đã cập nhật giá cho {affectedRows} dịch vụ (mỗi dịch vụ là 1 bản ghi danh mục). "
                                + $"{ids.Count - affectedRows} dịch vụ được bỏ qua do giá không còn hợp lệ hoặc không thay đổi."
                    });
                }
                catch
                {
                    await tran.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ConfirmGiaResponse
                {
                    thanhCong = false,
                    soDichVuCapNhat = 0,
                    message = "Lỗi server, đã hủy toàn bộ thay đổi (rollback). Chi tiết: " + ex.Message
                });
            }
        }
    }

    public class PreviewGiaItem
    {
        public int dichvuid { get; set; }
        public string? maDichVu { get; set; }
        public string? tenDichVu { get; set; }
        public int? giaHienTai { get; set; }
        public long? giaMoi { get; set; }
        public DateTime? ngayYl { get; set; }
        public string trangThai { get; set; } = "KHONG_CO_NGUON"; // THAY_DOI | KHONG_DOI | KHONG_CO_NGUON
    }

    public class PreviewGiaResponse
    {
        public int tongSoDichVu { get; set; }
        public int soCoNguon { get; set; }
        public int soGiaThayDoi { get; set; }
        public int soGiaKhongDoi { get; set; }
        public int soKhongCoNguon { get; set; }
        public List<PreviewGiaItem> danhSach { get; set; } = new();
    }

    public class ConfirmCapNhatGiaRequest
    {
        public List<int> DichVuIds { get; set; } = new();
    }

    public class ConfirmGiaResponse
    {
        public bool thanhCong { get; set; }
        public int soDichVuCapNhat { get; set; }
        public int soBoQua { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class DsDichVuRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public int? IdLoaiDV { get; set; }
    }

    public class CapNhatDichVuRequest
    {
        public int DichVuId { get; set; }
        public decimal? ChiPhi { get; set; }
        public decimal? HeSo { get; set; }
        public decimal? HeSo_Cls_bs { get; set; }
        public decimal? HeSo_Cls_dd{ get; set; }
    }
}