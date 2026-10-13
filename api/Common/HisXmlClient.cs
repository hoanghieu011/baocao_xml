using api.DTO;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace API.Common;

// Một client/phiên gọi; không chia sẻ cookie giữa các bệnh viện.
public sealed class HisXmlClient : IDisposable
{
  private const string Endpoint = "https://bvdakhoa.vncare.vn/vnpthis/RestService";
  private readonly HttpClient client;

  public HisXmlClient(HttpMessageHandler? handler = null)
  {
    client = new HttpClient(handler ?? new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
    { Timeout = TimeSpan.FromSeconds(120), MaxResponseContentBufferSize = 50 * 1024 * 1024 };
  }

  private static string Json(object value) => JsonSerializer.Serialize(value);

  public async Task<HisSessionStatus> CheckSession(HisSessionRequest credentials, CancellationToken ct)
  {
    // Không dùng danh sách hồ sơ theo tháng để xác nhận phiên: cookie hết hạn
    // có thể khiến HIS trả danh sách rỗng giống tháng không có bệnh nhân.
    var request = new HisFetchRequest { Token = credentials.Token, Cookie = credentials.Cookie };
    var raw = await Send(request, new { func = "ajaxCALL_SP_O", @params = new object[] { "DMC70.04", -1, 0 }, uuid = credentials.Token.Trim() }, null, ct, plainText: true);
    using var document = JsonDocument.Parse(raw);
    var root = document.RootElement;
    // Chỉ xác nhận khi truy vấn khoa/phòng trả dữ liệu; null/rỗng/trang login
    // và các cấu trúc chưa biết đều không đủ bằng chứng để mở chức năng.
    var rows = root;
    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("rows", out var wrapped)) rows = wrapped;
    var valid = rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() > 0 &&
      rows.EnumerateArray().All(row => row.ValueKind == JsonValueKind.Object &&
        row.EnumerateObject().Any(p => p.Name.Contains("ID", StringComparison.OrdinalIgnoreCase) &&
          p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number && !string.IsNullOrWhiteSpace(p.Value.ToString())) &&
        !row.EnumerateObject().Any(p => p.Name.Equals("error", StringComparison.OrdinalIgnoreCase)));
    return new HisSessionStatus(valid, valid ? "Phiên HIS hợp lệ. Có thể fetch và import XML." :
      "Chưa xác minh được phiên HIS: truy vấn khoa/phòng trả rỗng hoặc cấu trúc chưa được hỗ trợ. Hãy lấy lại token/cookie và kiểm tra phản hồi DMC70.04.");
  }

  private static (string From, string To) Dates(HisFetchRequest r) => (
    new DateTime(r.Year, r.Month, 1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " 00:00:00",
    new DateTime(r.Year, r.Month, DateTime.DaysInMonth(r.Year, r.Month)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " 23:59:59");

  private async Task<string> Send(HisFetchRequest r, object payload, string? query, CancellationToken ct, bool plainText = false)
  {
    for (var attempt = 0; ; attempt++)
    {
      using var request = new HttpRequestMessage(query == null ? HttpMethod.Post : HttpMethod.Get,
        Endpoint + (query == null ? "" : "?postData=" + Uri.EscapeDataString(Json(payload)) + query));
      request.Headers.Add("Cookie", r.Cookie.Trim());
      request.Headers.Add("X-Requested-With", "XMLHttpRequest");
      request.Headers.Referrer = new Uri("https://bvdakhoa.vncare.vn/vnpthis/main/manager.jsp?func=../noitru/NTU02D060_GuiBaoHiem4210");
      request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
      if (query == null) request.Content = new StringContent(Json(payload), Encoding.UTF8, plainText ? "text/plain" : "application/json");
      try
      {
        using var response = await client.SendAsync(request, ct);
        var status = (int)response.StatusCode;
        if (status is 429 or 502 or 503 or 504 && attempt < 2)
        {
          await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), ct);
          continue;
        }
        if (status is 401 or 403 || status is >= 300 and < 400)
          throw new InvalidDataException("Phiên HIS hết hạn. Vui lòng lấy lại token và cookie.");
        if (!response.IsSuccessStatusCode)
          throw new InvalidDataException($"HIS trả HTTP {status}. Kiểm tra phiên đăng nhập và tham số lấy dữ liệu.");
        return (await response.Content.ReadAsStringAsync(ct)).Trim().TrimStart('\uFEFF');
      }
      catch (HttpRequestException) when (attempt < 2)
      {
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), ct);
      }
    }
  }

  public async Task<HisPage> FetchPage(HisFetchRequest r, CancellationToken ct)
  {
    var dates = Dates(r);
    var options = new Dictionary<string, string>
    {
      ["TU_NGAY"] = dates.From, ["DEN_NGAY"] = dates.To, ["DS_MALK"] = "",
      ["LOCTHEO"] = r.LocTheo, ["LOAIXML"] = r.LoaiXml, ["DTBNID"] = r.Dtbnid,
      ["CSKCB"] = r.Cskcb, ["DS_BA"] = ""
    };
    foreach (var key in new[] { "TUYEN", "LOAITHE", "LOAIHS", "MATHE", "PHAMVI", "MUCHUONG", "NHOMBHXH", "NHOMDV", "KHOA", "PHONG", "LOAITIEPNHAN", "GUI_CONG", "GUI_BHXH", "TRANGTHAI_KYSO" })
      options[key] = "-1";
    var payload = new { func = "ajaxExecuteQueryPaging", uuid = r.Token.Trim(), @params = new[] { "NTU02D061.03.RG" }, options = new[] { new { name = "[0]", value = Json(options) } } };
    var raw = await Send(r, payload, $"&_search=false&nd={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&rows={r.Rows}&page={r.Page}&sidx=&sord=asc", ct);
    using var doc = JsonDocument.Parse(raw);
    var root = doc.RootElement;
    if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("records", out var totalValue) ||
        !int.TryParse(totalValue.ToString(), out var total) || total < 0)
      throw new InvalidDataException("HIS từ chối truy vấn. Kiểm tra CSKCB, LOCTHEO, LOAIXML và DTBNID.");
    var records = new List<HisRecord>();
    if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
    {
      foreach (var row in rows.EnumerateArray())
      {
        if (!row.TryGetProperty("MA_LK", out var id) || string.IsNullOrWhiteSpace(id.ToString()))
          throw new InvalidDataException("Danh sách HIS có hồ sơ thiếu MA_LK.");
        var transaction = row.TryGetProperty("MA_GIAO_DICH_2", out var code) ? code.ToString() : "";
        if (transaction.Length == 0 && row.TryGetProperty("MA_GIAO_DICH", out code)) transaction = code.ToString();
        records.Add(new HisRecord(id.ToString(), transaction.Contains("3176") ? "1" : transaction.Contains("4750") ? "0" : "auto"));
      }
    }
    if (total > 0 && records.Count == 0)
      throw new InvalidDataException("HIS trả trang rỗng dù còn hồ sơ. Vui lòng kiểm tra cookie và thử lại.");
    return new HisPage(total, records);
  }

  public async Task<byte[]> Export(HisFetchRequest r, CancellationToken ct)
  {
    var dates = Dates(r);
    var expected = r.MaLks.ToHashSet();
    var collected = new Dictionary<string, XElement>();
    XDocument? merged = null;
    XElement? container = null;
    foreach (var qd in r.Qd3176 == "0" ? new[] { "0", "1" } : new[] { "1", "0" })
    {
      var parameters = new { LOCTHEO = "1", TU_NGAY = dates.From, DEN_NGAY = dates.To,
        LoaiHS = "-1", Tuyen = "-1", MaThe = "-1", LoaiThe = "-1", MAHOA = "1",
        DS_MALK = string.Join(",", r.MaLks), HCODE = r.Cskcb, MODE = "1", DTBN = r.Dtbnid,
        LOAIXML = r.LoaiXml, QD3176 = qd };
      var raw = await Send(r, new { func = "ajaxCALL_SP_X", @params = new[] { "XUAT.XML", Json(parameters) }, uuid = r.Token.Trim() }, null, ct);
      if (raw is "null" or "\"null\"") continue;
      if (!raw.StartsWith('<'))
      {
        using var json = JsonDocument.Parse(raw);
        raw = FindXml(json.RootElement) ?? throw new InvalidDataException("HIS không trả nội dung XML.");
      }
      var document = ParseXml(raw);
      var records = document.Descendants("HOSO").ToList();
      if (records.Count == 0) throw new InvalidDataException("HIS trả XML không có hồ sơ HOSO.");
      var ids = new HashSet<string>();
      foreach (var record in records)
      {
        var files = record.Elements("FILEHOSO").Where(x => (string?)x.Element("LOAIHOSO") == "XML1").ToList();
        if (files.Count != 1) throw new InvalidDataException("Hồ sơ phải có đúng một phần XML1.");
        var file = files[0];
        var content = file.Element("NOIDUNGFILE") ?? throw new InvalidDataException("XML thiếu NOIDUNGFILE.");
        var xml = DecodeContent(content);
        var id = (string?)xml.Element("MA_LK") ?? "";
        if (!expected.Contains(id) || !ids.Add(id)) throw new InvalidDataException("XML có MA_LK trùng hoặc ngoài lô yêu cầu.");
        if ((string?)xml.Element("MA_CSKCB") != r.Cskcb)
          throw new InvalidDataException("Mã cơ sở trong XML không khớp CSKCB đã chọn. Chưa import lô này.");
        collected.TryAdd(id, new XElement(record));
      }
      if (ids.SetEquals(expected)) return Encoding.UTF8.GetBytes(raw);
      if (merged == null)
      {
        merged = document;
        container = records[0].Parent ?? throw new InvalidDataException("XML thiếu danh sách hồ sơ.");
        records.ForEach(record => record.Remove());
      }
      if (collected.Count == expected.Count)
      {
        // Một lô 500 mã có thể chứa cả hai QĐ: lấy phần còn thiếu bằng cờ còn lại.
        container!.Add(r.MaLks.Select(id => collected[id]));
        foreach (var count in merged.Descendants("SOLUONGHOSO")) count.Value = collected.Count.ToString(CultureInfo.InvariantCulture);
        var bytes = Encoding.UTF8.GetBytes(merged.ToString());
        if (bytes.Length > 50 * 1024 * 1024) throw new InvalidDataException("Lô XML vượt quá giới hạn 50 MB.");
        return bytes;
      }
    }
    throw new InvalidDataException($"HIS chỉ trả {collected.Count}/{expected.Count} hồ sơ sau khi tự dò hai QĐ. Chưa lưu/import lô này; hãy kiểm tra phiên và thử lại.");
  }

  public static XElement DecodeContent(XElement content) => content.Elements().FirstOrDefault() ??
    ParseXml(Encoding.UTF8.GetString(Convert.FromBase64String(content.Value)).TrimStart('\uFEFF')).Root
      ?? throw new InvalidDataException("NOIDUNGFILE không có XML.");

  private static XDocument ParseXml(string value)
  {
    using var reader = XmlReader.Create(new StringReader(value), new XmlReaderSettings
    { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 50 * 1024 * 1024 });
    return XDocument.Load(reader);
  }

  private static string? FindXml(JsonElement value)
  {
    if (value.ValueKind == JsonValueKind.String) return value.GetString()?.TrimStart() is { } s && s.StartsWith('<') ? s : null;
    if (value.ValueKind == JsonValueKind.Object)
      foreach (var p in value.EnumerateObject()) { var found = FindXml(p.Value); if (found != null) return found; }
    if (value.ValueKind == JsonValueKind.Array)
      foreach (var item in value.EnumerateArray()) { var found = FindXml(item); if (found != null) return found; }
    return null;
  }

  public void Dispose() => client.Dispose();
}
