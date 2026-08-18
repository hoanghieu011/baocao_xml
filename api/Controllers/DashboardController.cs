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

                return Ok(response);
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
