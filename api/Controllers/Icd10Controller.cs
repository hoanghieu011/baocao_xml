using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Icd10Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpFactory;

        // Nguồn dữ liệu ICD-10 (cổng tra cứu ICD Việt Nam - Bộ Y tế / icd.kcb.vn)
        private const string SOURCE_BASE = "https://ccs.whiteneuron.com/api/ICD10";
        private const string SOURCE_NAME = "icd.kcb.vn";
        // Số request song song tối đa khi duyệt cây danh mục
        private const int MAX_CONCURRENCY = 10;

        public Icd10Controller(ApplicationDbContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpFactory = httpFactory;
        }

        /// <summary>
        /// Tra cứu danh mục ICD-10 (phân trang, tìm theo mã hoặc tên).
        /// </summary>
        [Authorize]
        [HttpPost("ds_icd10")]
        public async Task<ActionResult<object>> GetDsIcd10([FromBody] DsIcd10Request req)
        {
            try
            {
                if (req == null)
                    return BadRequest("Yêu cầu không hợp lệ.");

                await EnsureTableAsync();

                var pageNumber = Math.Max(1, req.PageNumber);
                var pageSize = Math.Clamp(req.PageSize, 1, 1000);
                var offset = (pageNumber - 1) * pageSize;

                var whereBuilder = new StringBuilder(" WHERE TRANGTHAI = 1");
                var paramList = new List<DbParameter>();

                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                if (!string.IsNullOrWhiteSpace(req.SearchTerm) && req.SearchTerm != "All")
                {
                    var p = tempCmd.CreateParameter();
                    p.ParameterName = "@search";
                    p.Value = $"%{req.SearchTerm.Trim()}%";
                    paramList.Add(p);
                    whereBuilder.Append(" AND (MA_ICD LIKE @search OR TEN_ICD LIKE @search)");
                }

                // Mặc định chỉ hiển thị mã bệnh cụ thể (node lá); truyền chiTiet=false để xem cả nhóm/chương
                if (req.ChiTiet != false)
                {
                    whereBuilder.Append(" AND IS_LEAF = 1");
                }

                var sql = @"SELECT ICD10ID, MA_ICD, MA_ID, TEN_ICD, LOAI, MA_CHA, IS_LEAF, CAP, TRANGTHAI, NGAYTAO, NGUON
                            FROM dmc_icd10"
                          + whereBuilder.ToString()
                          + $" ORDER BY MA_ICD LIMIT {pageSize} OFFSET {offset}";

                var dsIcd10 = await _context.dmc_icd10
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                int totalRecords;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(1) FROM dmc_icd10" + whereBuilder.ToString();
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
                    DsIcd10 = dsIcd10
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Lấy các node con của một node trong cây danh mục ICD-10.
        /// Không truyền maCha (hoặc rỗng) => trả về danh sách chương gốc (22 chương).
        /// </summary>
        [Authorize]
        [HttpPost("cay_con")]
        public async Task<ActionResult<object>> GetCayCon([FromBody] CayConRequest req)
        {
            try
            {
                await EnsureTableAsync();

                var paramList = new List<DbParameter>();
                var conn = _context.Database.GetDbConnection();
                using var tempCmd = conn.CreateCommand();

                string whereCha;
                if (string.IsNullOrWhiteSpace(req?.MaCha))
                {
                    // Node gốc: MA_CHA rỗng/null (các chương)
                    whereCha = " AND (MA_CHA IS NULL OR MA_CHA = '')";
                }
                else
                {
                    var p = tempCmd.CreateParameter();
                    p.ParameterName = "@maCha";
                    p.Value = req.MaCha.Trim();
                    paramList.Add(p);
                    whereCha = " AND MA_CHA = @maCha";
                }

                var sql = @"SELECT ICD10ID, MA_ICD, MA_ID, TEN_ICD, LOAI, MA_CHA, IS_LEAF, CAP, TRANGTHAI, NGAYTAO, NGUON
                            FROM dmc_icd10 WHERE TRANGTHAI = 1"
                          + whereCha
                          + " ORDER BY MA_ICD";

                var nodes = await _context.dmc_icd10
                    .FromSqlRaw(sql, paramList.ToArray())
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new { DsIcd10 = nodes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Tìm kiếm theo mã/tên, trả về các node khớp KÈM toàn bộ tổ tiên của chúng
        /// để frontend dựng lại cây (mở tới node con thấp nhất chứa từ khóa).
        /// </summary>
        [Authorize]
        [HttpPost("tim_cay")]
        public async Task<ActionResult<object>> TimCay([FromBody] DsIcd10Request req)
        {
            try
            {
                await EnsureTableAsync();

                var term = req?.SearchTerm?.Trim();
                if (string.IsNullOrWhiteSpace(term))
                    return Ok(new { DsIcd10 = new List<Icd10>(), Truncated = false });

                const int MAX_MATCH = 500;
                const string COLS = "ICD10ID, MA_ICD, MA_ID, TEN_ICD, LOAI, MA_CHA, IS_LEAF, CAP, TRANGTHAI, NGAYTAO, NGUON";

                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // 1. Các node khớp từ khóa (giới hạn để tránh quá tải)
                var matchSql = $@"SELECT {COLS} FROM dmc_icd10
                                  WHERE TRANGTHAI = 1 AND (MA_ICD LIKE @search OR TEN_ICD LIKE @search)
                                  ORDER BY MA_ICD LIMIT {MAX_MATCH + 1}";
                var searchParam = new MySqlParameter("@search", $"%{term}%");
                var matches = await _context.dmc_icd10
                    .FromSqlRaw(matchSql, searchParam)
                    .AsNoTracking()
                    .ToListAsync();

                var truncated = matches.Count > MAX_MATCH;
                if (truncated) matches = matches.Take(MAX_MATCH).ToList();

                // 2. Thu thập toàn bộ tổ tiên theo MA_CHA cho tới gốc
                var byId = new Dictionary<string, Icd10>();
                foreach (var m in matches)
                    byId[m.ma_id] = m;

                var pending = matches
                    .Select(m => m.ma_cha)
                    .Where(c => !string.IsNullOrEmpty(c) && !byId.ContainsKey(c!))
                    .Distinct()
                    .ToList();

                var guard = 0;
                while (pending.Count > 0 && guard++ < 20)
                {
                    var placeholders = string.Join(",", pending.Select((_, i) => $"@p{i}"));
                    var ancSql = $"SELECT {COLS} FROM dmc_icd10 WHERE MA_ID IN ({placeholders})";
                    var ancParams = pending
                        .Select((v, i) => new MySqlParameter($"@p{i}", v))
                        .ToArray();

                    var parents = await _context.dmc_icd10
                        .FromSqlRaw(ancSql, ancParams)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var p in parents)
                        byId[p.ma_id] = p;

                    pending = parents
                        .Select(p => p.ma_cha)
                        .Where(c => !string.IsNullOrEmpty(c) && !byId.ContainsKey(c!))
                        .Distinct()
                        .ToList();
                }

                var nodes = byId.Values
                    .OrderBy(n => n.ma_icd)
                    .ToList();

                return Ok(new { DsIcd10 = nodes, Truncated = truncated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", detail = ex.Message });
            }
        }

        /// <summary>
        /// Đồng bộ danh mục ICD-10 từ nguồn (icd.kcb.vn) vào bảng dmc_icd10.
        /// Duyệt toàn bộ cây danh mục và upsert theo mã định danh.
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("DongBoTuNguon")]
        public async Task<ActionResult<object>> DongBoTuNguon()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await EnsureTableAsync();

                var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(60);

                var allNodes = new ConcurrentBag<Icd10>();
                var ngayTao = DateTime.Now;

                // Cấp 1: các chương (root)
                var chapters = await FetchNodesAsync(http, $"{SOURCE_BASE}/root");
                var frontier = new List<Icd10>();
                foreach (var n in chapters)
                {
                    var node = MapNode(n, null, 1, ngayTao);
                    allNodes.Add(node);
                    if (n.is_leaf != true) frontier.Add(node);
                }

                // Duyệt theo từng cấp, gọi con song song có giới hạn
                var cap = 1;
                while (frontier.Count > 0 && cap < 15)
                {
                    cap++;
                    var nextFrontier = new ConcurrentBag<Icd10>();
                    using var gate = new SemaphoreSlim(MAX_CONCURRENCY);

                    var tasks = frontier.Select(async parent =>
                    {
                        await gate.WaitAsync();
                        try
                        {
                            var url = $"{SOURCE_BASE}/childs/{parent.loai}?id={Uri.EscapeDataString(parent.ma_id)}";
                            var childs = await FetchNodesAsync(http, url);
                            foreach (var c in childs)
                            {
                                var childNode = MapNode(c, parent.ma_id, cap, ngayTao);
                                allNodes.Add(childNode);
                                if (c.is_leaf != true) nextFrontier.Add(childNode);
                            }
                        }
                        finally
                        {
                            gate.Release();
                        }
                    });
                    await Task.WhenAll(tasks);

                    frontier = nextFrontier.ToList();
                }

                var nodes = allNodes
                    .GroupBy(n => n.ma_id)
                    .Select(g => g.First())
                    .ToList();

                var affected = await UpsertNodesAsync(nodes);

                sw.Stop();
                return Ok(new
                {
                    message = "Đồng bộ danh mục ICD-10 thành công.",
                    tongSoNode = nodes.Count,
                    soMaBenh = nodes.Count(n => n.is_leaf == 1),
                    soChuong = chapters.Count,
                    soBanGhiCapNhat = affected,
                    thoiGianGiay = Math.Round(sw.Elapsed.TotalSeconds, 1)
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return StatusCode(500, new { message = "Lỗi đồng bộ ICD-10", detail = ex.Message });
            }
        }

        // ---------- Helpers ----------

        private Icd10 MapNode(SourceNode n, string? maCha, int cap, DateTime ngayTao)
        {
            return new Icd10
            {
                ma_id = n.id ?? n.data?.id ?? "",
                ma_icd = n.data?.code,
                ten_icd = n.data?.name,
                loai = n.model,
                ma_cha = maCha,
                is_leaf = n.is_leaf == true ? 1 : 0,
                cap = cap,
                trangthai = 1,
                ngaytao = ngayTao,
                nguon = SOURCE_NAME
            };
        }

        private async Task<List<SourceNode>> FetchNodesAsync(HttpClient http, string url)
        {
            try
            {
                var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<SourceNode>();
                var stream = await resp.Content.ReadAsStreamAsync();
                var parsed = await JsonSerializer.DeserializeAsync<SourceResponse>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return parsed?.data ?? new List<SourceNode>();
            }
            catch
            {
                // Bỏ qua node lỗi để không dừng toàn bộ quá trình đồng bộ
                return new List<SourceNode>();
            }
        }

        private async Task<int> UpsertNodesAsync(List<Icd10> nodes)
        {
            if (nodes.Count == 0) return 0;

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var affected = 0;
            const int batchSize = 500;

            for (int start = 0; start < nodes.Count; start += batchSize)
            {
                var batch = nodes.Skip(start).Take(batchSize).ToList();

                using var cmd = conn.CreateCommand();
                var sb = new StringBuilder(@"INSERT INTO dmc_icd10
                    (MA_ID, MA_ICD, TEN_ICD, LOAI, MA_CHA, IS_LEAF, CAP, TRANGTHAI, NGAYTAO, NGUON) VALUES ");

                for (int i = 0; i < batch.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append($"(@ma_id{i},@ma_icd{i},@ten{i},@loai{i},@cha{i},@la{i},@cap{i},1,@ngay{i},@nguon{i})");

                    AddParam(cmd, $"@ma_id{i}", batch[i].ma_id);
                    AddParam(cmd, $"@ma_icd{i}", (object?)batch[i].ma_icd ?? DBNull.Value);
                    AddParam(cmd, $"@ten{i}", (object?)batch[i].ten_icd ?? DBNull.Value);
                    AddParam(cmd, $"@loai{i}", (object?)batch[i].loai ?? DBNull.Value);
                    AddParam(cmd, $"@cha{i}", (object?)batch[i].ma_cha ?? DBNull.Value);
                    AddParam(cmd, $"@la{i}", batch[i].is_leaf ?? 0);
                    AddParam(cmd, $"@cap{i}", batch[i].cap ?? 0);
                    AddParam(cmd, $"@ngay{i}", batch[i].ngaytao ?? DateTime.Now);
                    AddParam(cmd, $"@nguon{i}", (object?)batch[i].nguon ?? DBNull.Value);
                }

                sb.Append(@" ON DUPLICATE KEY UPDATE
                    MA_ICD=VALUES(MA_ICD), TEN_ICD=VALUES(TEN_ICD), LOAI=VALUES(LOAI),
                    MA_CHA=VALUES(MA_CHA), IS_LEAF=VALUES(IS_LEAF), CAP=VALUES(CAP),
                    TRANGTHAI=1, NGAYTAO=VALUES(NGAYTAO), NGUON=VALUES(NGUON)");

                cmd.CommandText = sb.ToString();
                affected += await cmd.ExecuteNonQueryAsync();
            }

            return affected;
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private async Task EnsureTableAsync()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS dmc_icd10 (
                ICD10ID INT AUTO_INCREMENT PRIMARY KEY,
                MA_ID VARCHAR(32) NOT NULL,
                MA_ICD VARCHAR(32) NULL,
                TEN_ICD VARCHAR(512) NULL,
                LOAI VARCHAR(32) NULL,
                MA_CHA VARCHAR(32) NULL,
                IS_LEAF TINYINT DEFAULT 0,
                CAP INT DEFAULT 0,
                TRANGTHAI TINYINT DEFAULT 1,
                NGAYTAO DATETIME NULL,
                NGUON VARCHAR(64) NULL,
                UNIQUE KEY uq_ma_id (MA_ID),
                KEY idx_ma_icd (MA_ICD),
                KEY idx_ten_icd (TEN_ICD(191))
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            await _context.Database.ExecuteSqlRawAsync(sql);

            // Đổi tên cột cũ LA_LA -> IS_LEAF cho các bảng đã tạo trước đó (idempotent).
            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = @"SELECT COLUMN_NAME FROM information_schema.COLUMNS
                                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'dmc_icd10'
                                       AND COLUMN_NAME IN ('LA_LA','IS_LEAF')";
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = await checkCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    cols.Add(reader.GetString(0));
            }

            if (cols.Contains("LA_LA") && !cols.Contains("IS_LEAF"))
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE dmc_icd10 CHANGE COLUMN LA_LA IS_LEAF TINYINT DEFAULT 0");
            }
        }

        // ---------- DTO nguồn ----------

        public class SourceResponse
        {
            public string? status { get; set; }
            public List<SourceNode>? data { get; set; }
        }

        public class SourceNode
        {
            public string? model { get; set; }
            public string? id { get; set; }
            [JsonPropertyName("is_leaf")]
            public bool? is_leaf { get; set; }
            public SourceNodeData? data { get; set; }
        }

        public class SourceNodeData
        {
            public string? id { get; set; }
            public string? code { get; set; }
            public string? name { get; set; }
        }
    }

    public class CayConRequest
    {
        // Mã định danh node cha (ma_id). Rỗng/null => lấy các chương gốc.
        public string? MaCha { get; set; }
    }

    public class DsIcd10Request
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        // true (mặc định): chỉ mã bệnh cụ thể; false: hiển thị cả nhóm/chương
        public bool? ChiTiet { get; set; } = true;
    }
}
