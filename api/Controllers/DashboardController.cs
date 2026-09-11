using API.Common;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseResolver _dbResolver;

        public DashboardController(ApplicationDbContext context, DatabaseResolver dbResolver)
        {
            _context = context;
            _dbResolver = dbResolver;
        }

        /// <summary>
        /// Lấy dữ liệu dashboard theo năm.
        /// </summary>
        [Authorize]
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryResponse>> GetSummary([FromQuery] int year)
        {
            try
            {
                if (year < 2000 || year > DateTime.Now.Year + 1)
                    return BadRequest("Năm không hợp lệ.");

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var columnMapping = await GetColumnMappingAsync(conn, dbData);
                if (!columnMapping.IsValid)
                {
                    return BadRequest("Thiếu cột dữ liệu bắt buộc của bảng xml_bnnd để tổng hợp dashboard.");
                }

                var kpi = await GetKpiDataAsync(conn, dbData, year, columnMapping);
                var monthlyVisitsMap = await GetMonthlyVisitsAsync(conn, dbData, year, columnMapping);
                var monthlyRevenueMap = await GetMonthlyRevenueAsync(conn, dbData, year, columnMapping);

                var monthlyVisits = new List<long>(12);
                var bhytPaidByMonth = new List<decimal>(12);
                var copayByMonth = new List<decimal>(12);
                var insuredRevenueByMonth = new List<decimal>(12);
                var hospitalFeeByMonth = new List<decimal>(12);

                for (var month = 1; month <= 12; month++)
                {
                    monthlyVisits.Add(monthlyVisitsMap.TryGetValue(month, out var visits) ? visits : 0);

                    if (monthlyRevenueMap.TryGetValue(month, out var revenue))
                    {
                        bhytPaidByMonth.Add(revenue.BhytPaid);
                        copayByMonth.Add(revenue.Copay);
                        insuredRevenueByMonth.Add(revenue.BhytPaid + revenue.Copay);
                        hospitalFeeByMonth.Add(revenue.HospitalFee);
                    }
                    else
                    {
                        bhytPaidByMonth.Add(0);
                        copayByMonth.Add(0);
                        insuredRevenueByMonth.Add(0);
                        hospitalFeeByMonth.Add(0);
                    }
                }

                var response = new DashboardSummaryResponse
                {
                    Year = year,
                    Kpis = new DashboardKpis
                    {
                        TotalVisits = kpi.InsuredVisitCount + kpi.HospitalFeeVisitCount,
                        InsuredPatients = kpi.InsuredVisitCount,
                        TotalRevenue = kpi.TotalRevenue
                    },
                    RevenueStructure = new DashboardRevenueStructure
                    {
                        BhytPaid = bhytPaidByMonth.Sum(),
                        Copay = copayByMonth.Sum(),
                        HospitalFee = hospitalFeeByMonth.Sum()
                    },
                    MonthlyVisits = monthlyVisits,
                    MonthlyRevenue = new DashboardMonthlyRevenue
                    {
                        BhytPaid = bhytPaidByMonth,
                        Copay = copayByMonth,
                        InsuredPatientRevenue = insuredRevenueByMonth,
                        HospitalFee = hospitalFeeByMonth
                    }
                };

                response.DiseaseChapters = await GetDiseaseChaptersAsync(conn, dbData, year);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê số lượng dịch vụ kỹ thuật theo năm, có phân trang (toàn bộ danh sách).
        /// </summary>
        [Authorize]
        [HttpGet("technical-services")]
        public async Task<ActionResult<object>> GetTechnicalServices(
            [FromQuery] int year, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (year < 2000 || year > DateTime.Now.Year + 1)
                    return BadRequest("Năm không hợp lệ.");

                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 1000);

                var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

                if (string.IsNullOrEmpty(userName))
                    return Unauthorized();

                var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
                if (string.IsNullOrEmpty(dbData))
                    return BadRequest("Không xác định được database dữ liệu cho user.");

                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                var columnMapping = await GetColumnMappingAsync(conn, dbData);
                if (!columnMapping.IsValid)
                    return BadRequest("Thiếu cột dữ liệu bắt buộc của bảng xml_bnnd để tổng hợp dịch vụ kỹ thuật.");

                var (total, items) = await GetTechnicalServicesAsync(conn, dbData, year, columnMapping, pageNumber, pageSize);

                return Ok(new
                {
                    TotalRecords = total,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        private static void AddYearParameter(DbCommand cmd, int year)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = "@year";
            p.Value = year;
            cmd.Parameters.Add(p);
        }

        private static long ToInt64(object? value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToInt64(value);
        }

        private static decimal ToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }

        private async Task<DashboardKpiRaw> GetKpiDataAsync(DbConnection conn, string dbName, int year, DashboardColumnMapping mapping)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
SELECT
    (SELECT COUNT(1) FROM `{dbName}`.xml1 WHERE YEAR(NGAY_VAO) = @year) AS sl_bnbh,
    (SELECT COUNT(1) FROM `{dbName}`.xml_bnnd WHERE YEAR(`{mapping.VisitDateColumn}`) = @year) AS sl_bnvp,
    (SELECT COALESCE(SUM(`{mapping.HospitalFeeAmountColumn}`), 0) FROM `{dbName}`.xml_bnnd WHERE YEAR(`{mapping.RevenueDateColumn}`) = @year) AS vp_tien_nhandan,
    (SELECT COALESCE(SUM(T_BNCCT), 0) FROM `{dbName}`.xml2 WHERE YEAR(NGAY_YL) = @year) AS bncct_xml2,
    (SELECT COALESCE(SUM(T_BNCCT), 0) FROM `{dbName}`.xml3 WHERE YEAR(NGAY_YL) = @year) AS bncct_xml3,
    (SELECT COALESCE(SUM(T_BHTT), 0) FROM `{dbName}`.xml2 WHERE YEAR(NGAY_YL) = @year) AS bhtt_xml2,
    (SELECT COALESCE(SUM(T_BHTT), 0) FROM `{dbName}`.xml3 WHERE YEAR(NGAY_YL) = @year) AS bhtt_xml3;
";
            AddYearParameter(cmd, year);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new DashboardKpiRaw();
            }

            var insuredVisitCount = ToInt64(reader["sl_bnbh"]);
            var hospitalFeeVisitCount = ToInt64(reader["sl_bnvp"]);
            var vpTienNhanDan = ToDecimal(reader["vp_tien_nhandan"]);
            var bncct = ToDecimal(reader["bncct_xml2"]) + ToDecimal(reader["bncct_xml3"]);
            var bhtt = ToDecimal(reader["bhtt_xml2"]) + ToDecimal(reader["bhtt_xml3"]);

            return new DashboardKpiRaw
            {
                InsuredVisitCount = insuredVisitCount,
                HospitalFeeVisitCount = hospitalFeeVisitCount,
                BhytPaidRevenue = bhtt,
                CopayRevenue = bncct,
                HospitalFeeRevenue = vpTienNhanDan,
                TotalRevenue = vpTienNhanDan + bncct + bhtt
            };
        }

        private async Task<Dictionary<int, long>> GetMonthlyVisitsAsync(DbConnection conn, string dbName, int year, DashboardColumnMapping mapping)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
SELECT
    t.thang,
    SUM(t.soluong) AS sl_bn
FROM (
    SELECT 1 AS soluong, CAST(MONTH(ngay_ra) AS SIGNED) AS thang
    FROM `{dbName}`.xml1
    WHERE YEAR(ngay_ra) = @year
    UNION ALL
    SELECT 1 AS soluong, CAST(MONTH(`{mapping.DischargeDateColumn}`) AS SIGNED) AS thang
    FROM `{dbName}`.xml_bnnd
    WHERE YEAR(`{mapping.DischargeDateColumn}`) = @year
) t
GROUP BY t.thang;
";
            AddYearParameter(cmd, year);

            var result = new Dictionary<int, long>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var month = Convert.ToInt32(reader["thang"]);
                var total = ToInt64(reader["sl_bn"]);
                if (month >= 1 && month <= 12)
                {
                    result[month] = total;
                }
            }

            return result;
        }

        private async Task<Dictionary<int, MonthlyRevenueRaw>> GetMonthlyRevenueAsync(DbConnection conn, string dbName, int year, DashboardColumnMapping mapping)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
SELECT
    m.thang,
    COALESCE(bh.t_bhtt, 0) AS t_bhtt,
    COALESCE(bh.t_bncct, 0) AS t_bncct,
    COALESCE(vp.t_vp, 0) AS t_vp
FROM (
    SELECT 1 AS thang UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL
    SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL
    SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
) m
LEFT JOIN (
    SELECT
        t.thang,
        SUM(t.t_bhtt) AS t_bhtt,
        SUM(t.t_bncct) AS t_bncct
    FROM (
        SELECT MONTH(bn.ngay_ra) AS thang, thuoc.t_bhtt, thuoc.t_bncct
        FROM `{dbName}`.xml1 bn
        INNER JOIN `{dbName}`.xml2 thuoc ON bn.ma_lk = thuoc.ma_lk
        WHERE YEAR(bn.ngay_ra) = @year
        UNION ALL
        SELECT MONTH(bn.ngay_ra) AS thang, dvkt.t_bhtt, dvkt.t_bncct
        FROM `{dbName}`.xml1 bn
        INNER JOIN `{dbName}`.xml3 dvkt ON bn.ma_lk = dvkt.ma_lk
        WHERE YEAR(bn.ngay_ra) = @year
    ) t
    GROUP BY t.thang
) bh ON bh.thang = m.thang
LEFT JOIN (
    SELECT
        MONTH(`{mapping.DischargeDateColumn}`) AS thang,
        SUM(`{mapping.HospitalFeeAmountColumn}`) AS t_vp
    FROM `{dbName}`.xml_bnnd
    WHERE YEAR(`{mapping.DischargeDateColumn}`) = @year
    GROUP BY MONTH(`{mapping.DischargeDateColumn}`)
) vp ON vp.thang = m.thang
ORDER BY m.thang;
";
            AddYearParameter(cmd, year);

            var result = new Dictionary<int, MonthlyRevenueRaw>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var month = Convert.ToInt32(reader["thang"]);
                if (month < 1 || month > 12)
                {
                    continue;
                }

                result[month] = new MonthlyRevenueRaw
                {
                    BhytPaid = ToDecimal(reader["t_bhtt"]),
                    Copay = ToDecimal(reader["t_bncct"]),
                    HospitalFee = ToDecimal(reader["t_vp"])
                };
            }

            return result;
        }

        /// <summary>
        /// Thống kê số lượng dịch vụ kỹ thuật (gộp bệnh nhân bảo hiểm xml3 + bệnh nhân viện phí xml_bnnd),
        /// loại các nhóm 10/13/15. Trả về toàn bộ danh sách có phân trang (Mã - Tên - Số lượng), sắp giảm dần.
        /// </summary>
        private async Task<(int Total, List<DashboardStatItem> Items)> GetTechnicalServicesAsync(
            DbConnection conn, string dbName, int year, DashboardColumnMapping mapping, int pageNumber, int pageSize)
        {
            var items = new List<DashboardStatItem>();
            var total = 0;

            // Truy vấn gộp (chưa phân trang) - dùng lại cho cả đếm tổng và lấy trang.
            var baseQuery = $@"
                SELECT madichvu, tendichvu, SUM(soluong) AS soluong
                FROM (
                    SELECT b.MA_DICH_VU AS madichvu, b.TEN_DICH_VU AS tendichvu, IFNULL(b.SO_LUONG, 0) AS soluong
                    FROM `{dbName}`.xml1 a
                    INNER JOIN `{dbName}`.xml3 b ON a.ma_lk = b.ma_lk
                    INNER JOIN his_common.dmc_nhom_mabhyt c ON b.ma_nhom = c.manhom_bhyt
                    WHERE YEAR(a.ngay_ra) = @year AND b.ma_nhom NOT IN (10, 13, 15)
                    UNION ALL
                    SELECT b.MADICHVU AS madichvu, b.TENDICHVU AS tendichvu, IFNULL(b.SOLUONG, 0) AS soluong
                    FROM `{dbName}`.xml_bnnd b
                    INNER JOIN his_common.dmc_nhom_mabhyt c ON b.NHOM_MABHYT_ID = c.manhom_bhyt
                    WHERE YEAR(b.`{mapping.DischargeDateColumn}`) = @year AND b.NHOM_MABHYT_ID NOT IN (10, 13, 15)
                ) t
                GROUP BY madichvu, tendichvu";

            try
            {
                using (var countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = $"SELECT COUNT(1) FROM ({baseQuery}) g;";
                    AddYearParameter(countCmd, year);
                    total = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                }

                var offset = (pageNumber - 1) * pageSize;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"{baseQuery} ORDER BY soluong DESC LIMIT {pageSize} OFFSET {offset};";
                AddYearParameter(cmd, year);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new DashboardStatItem
                    {
                        Ma = reader["madichvu"]?.ToString() ?? string.Empty,
                        Ten = reader["tendichvu"]?.ToString() ?? string.Empty,
                        SoLuong = ToDecimal(reader["soluong"])
                    });
                }
            }
            catch
            {
                // Bảng xml_bnnd có thể thiếu cột ở một số bệnh viện -> trả rỗng, không làm hỏng dashboard.
                return (0, new List<DashboardStatItem>());
            }

            return (total, items);
        }

        /// <summary>
        /// Thống kê số lượng bệnh tật theo chẩn đoán chính (xml1.ma_benh_chinh),
        /// gộp về 22 chương lớn của ICD-10. Trả về Mã (dải mã chương) - Tên chương - Số lượng.
        /// Danh mục chương lấy từ his_common.dmc_icd10 (đồng bộ qua Icd10Controller).
        /// Trả về đủ 22 chương (kể cả chương 0 ca), sắp xếp giảm dần theo số lượng.
        /// </summary>
        private async Task<List<DashboardStatItem>> GetDiseaseChaptersAsync(DbConnection conn, string dbName, int year)
        {
            // Danh mục chương lấy từ danh mục ICD-10 dùng chung (không hard-code)
            var chapters = await GetIcd10ChaptersAsync(conn);
            if (chapters.Count == 0)
            {
                return new List<DashboardStatItem>();
            }

            var totals = new long[chapters.Count];
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    SELECT UPPER(LEFT(ma_benh_chinh, 3)) AS ma3, COUNT(1) AS sl
                    FROM `{dbName}`.xml1
                    WHERE YEAR(ngay_ra) = @year AND ma_benh_chinh IS NOT NULL AND ma_benh_chinh <> ''
                    GROUP BY UPPER(LEFT(ma_benh_chinh, 3));";
                AddYearParameter(cmd, year);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var code = reader["ma3"]?.ToString() ?? string.Empty;
                    var index = FindChapterIndex(chapters, code);
                    if (index >= 0)
                    {
                        totals[index] += ToInt64(reader["sl"]);
                    }
                }
            }
            catch
            {
                return new List<DashboardStatItem>();
            }

            // Hiển thị đủ 22 chương (kể cả chương không phát sinh), sắp xếp giảm dần theo số lượng.
            var result = new List<DashboardStatItem>();
            for (var i = 0; i < chapters.Count; i++)
            {
                result.Add(new DashboardStatItem
                {
                    Ma = chapters[i].Range,
                    Ten = chapters[i].Name,
                    SoLuong = totals[i]
                });
            }

            return result
                .OrderByDescending(x => x.SoLuong)
                .ToList();
        }

        /// <summary>
        /// Lấy danh mục 22 chương ICD-10 từ his_common.dmc_icd10 — các node chương (CAP = 1).
        /// Lưu ý: với node chương, dải mã (vd "A00-B99") nằm ở cột MA_ID; MA_ICD là số La Mã;
        /// TEN_ICD có sẵn tiền tố "(A00-B99) ...".
        /// </summary>
        private async Task<List<Icd10Chapter>> GetIcd10ChaptersAsync(DbConnection conn)
        {
            var chapters = new List<Icd10Chapter>();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT MA_ID, TEN_ICD
                    FROM dmc_icd10
                    WHERE TRANGTHAI = 1 AND CAP = 1
                    ORDER BY MA_ID;";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var range = reader["MA_ID"]?.ToString()?.Trim() ?? string.Empty;
                    var name = reader["TEN_ICD"]?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(range)) continue;

                    // Bỏ tiền tố "(A00-B99) " trùng với cột Mã cho gọn.
                    if (name.StartsWith("("))
                    {
                        var idx = name.IndexOf(')');
                        if (idx >= 0) name = name[(idx + 1)..].Trim();
                    }

                    var (start, end) = ParseChapterRange(range);
                    chapters.Add(new Icd10Chapter(start, end, range, name));
                }
            }
            catch
            {
                // Danh mục chưa được đồng bộ / lỗi truy vấn -> trả rỗng, dashboard bỏ qua mục này.
                return new List<Icd10Chapter>();
            }

            return chapters;
        }

        // Tách dải mã chương "A00-B99" thành mã đầu/cuối (3 ký tự) để so sánh từ điển.
        private static (string Start, string End) ParseChapterRange(string range)
        {
            var parts = range.Split('-', 2, StringSplitOptions.TrimEntries);
            var start = NormalizeCode3(parts[0]);
            var end = NormalizeCode3(parts.Length > 1 ? parts[1] : parts[0]);
            return (start, end);
        }

        // Chuẩn hóa mã ICD về 3 ký tự đầu (vd "A00.0" -> "A00") để so khớp chương.
        private static string NormalizeCode3(string code)
        {
            var key = (code ?? string.Empty).Trim().ToUpperInvariant();
            return key.Length >= 3 ? key.Substring(0, 3) : key.PadRight(3, '0');
        }

        // Xác định chương ICD-10 của một mã bệnh theo 3 ký tự đầu (so sánh từ điển với dải mã chương).
        private static int FindChapterIndex(IReadOnlyList<Icd10Chapter> chapters, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return -1;
            var key = NormalizeCode3(code);

            for (var i = 0; i < chapters.Count; i++)
            {
                var ch = chapters[i];
                if (string.Compare(key, ch.Start, StringComparison.Ordinal) >= 0 &&
                    string.Compare(key, ch.End, StringComparison.Ordinal) <= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly record struct Icd10Chapter(string Start, string End, string Range, string Name);

        private async Task<DashboardColumnMapping> GetColumnMappingAsync(DbConnection conn, string dbName)
        {
            var visitDateColumn = await GetExistingColumnAsync(conn, dbName, "xml_bnnd", "NGAY_TIEPNHAN", "NGAY_RAVIEN");
            var dischargeDateColumn = await GetExistingColumnAsync(conn, dbName, "xml_bnnd", "NGAY_RAVIEN", "NGAY_TIEPNHAN");
            var revenueDateColumn = await GetExistingColumnAsync(conn, dbName, "xml_bnnd", "NGAYYLENH", "NGAY_YLENH", "NGAY_RAVIEN", "NGAY_TIEPNHAN");
            var hospitalFeeAmountColumn = await GetExistingColumnAsync(conn, dbName, "xml_bnnd", "TIEN_DANOP", "TIEN_NHANDAN");

            return new DashboardColumnMapping
            {
                VisitDateColumn = visitDateColumn ?? string.Empty,
                DischargeDateColumn = dischargeDateColumn ?? string.Empty,
                RevenueDateColumn = revenueDateColumn ?? string.Empty,
                HospitalFeeAmountColumn = hospitalFeeAmountColumn ?? string.Empty
            };
        }

        private async Task<string?> GetExistingColumnAsync(DbConnection conn, string dbName, string tableName, params string[] candidateColumns)
        {
            if (candidateColumns == null || candidateColumns.Length == 0)
            {
                return null;
            }

            using var cmd = conn.CreateCommand();

            var candidateParams = new List<string>();
            for (var i = 0; i < candidateColumns.Length; i++)
            {
                var parameterName = $"@c{i}";
                candidateParams.Add(parameterName);
                var p = cmd.CreateParameter();
                p.ParameterName = parameterName;
                p.Value = candidateColumns[i];
                cmd.Parameters.Add(p);
            }

            var dbParam = cmd.CreateParameter();
            dbParam.ParameterName = "@schema";
            dbParam.Value = dbName;
            cmd.Parameters.Add(dbParam);

            var tableParam = cmd.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            cmd.Parameters.Add(tableParam);

            var orderParts = new List<string>();
            for (var i = 0; i < candidateColumns.Length; i++)
            {
                orderParts.Add($"WHEN @c{i} THEN {i}");
            }

            cmd.CommandText = $@"
SELECT COLUMN_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = @schema
  AND TABLE_NAME = @table
  AND COLUMN_NAME IN ({string.Join(",", candidateParams)})
ORDER BY CASE COLUMN_NAME {string.Join(" ", orderParts)} ELSE 999 END
LIMIT 1;";

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return result.ToString();
        }
    }

    public class DashboardSummaryResponse
    {
        public int Year { get; set; }
        public DashboardKpis Kpis { get; set; } = new();
        public DashboardRevenueStructure RevenueStructure { get; set; } = new();
        public List<long> MonthlyVisits { get; set; } = new();
        public DashboardMonthlyRevenue MonthlyRevenue { get; set; } = new();
        // Thống kê số lượng bệnh tật gộp theo 22 chương ICD-10
        public List<DashboardStatItem> DiseaseChapters { get; set; } = new();
    }

    // Mục thống kê dạng đơn giản: Mã - Tên - Số lượng
    public class DashboardStatItem
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public decimal SoLuong { get; set; }
    }

    public class DashboardKpis
    {
        public long TotalVisits { get; set; }
        public long InsuredPatients { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DashboardRevenueStructure
    {
        public decimal BhytPaid { get; set; }
        public decimal Copay { get; set; }
        public decimal HospitalFee { get; set; }
    }

    public class DashboardMonthlyRevenue
    {
        public List<decimal> BhytPaid { get; set; } = new();
        public List<decimal> Copay { get; set; } = new();
        public List<decimal> InsuredPatientRevenue { get; set; } = new();
        public List<decimal> HospitalFee { get; set; } = new();
    }

    internal class DashboardKpiRaw
    {
        public long InsuredVisitCount { get; set; }
        public long HospitalFeeVisitCount { get; set; }
        public decimal BhytPaidRevenue { get; set; }
        public decimal CopayRevenue { get; set; }
        public decimal HospitalFeeRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    internal class MonthlyRevenueRaw
    {
        public decimal BhytPaid { get; set; }
        public decimal Copay { get; set; }
        public decimal HospitalFee { get; set; }
    }

    internal class DashboardColumnMapping
    {
        public string VisitDateColumn { get; set; } = string.Empty;
        public string DischargeDateColumn { get; set; } = string.Empty;
        public string RevenueDateColumn { get; set; } = string.Empty;
        public string HospitalFeeAmountColumn { get; set; } = string.Empty;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(VisitDateColumn) &&
            !string.IsNullOrWhiteSpace(DischargeDateColumn) &&
            !string.IsNullOrWhiteSpace(RevenueDateColumn) &&
            !string.IsNullOrWhiteSpace(HospitalFeeAmountColumn);
    }
}
