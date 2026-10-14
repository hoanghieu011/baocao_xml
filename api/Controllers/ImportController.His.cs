using api.DTO;
using API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Xml;
using System.Security.Claims;

namespace api.Controllers;

public partial class ImportController
{
  [Authorize(Roles = "ADMIN")]
  [HttpPost("CheckHisSession")]
  public Task<IActionResult> CheckHisSession([FromBody] HisSessionRequest request, CancellationToken ct) =>
    RunHis(async client => Ok(await client.CheckSession(request, ct)));

  [Authorize(Roles = "ADMIN")]
  [HttpPost("FetchHisXmlBatch")]
  public Task<IActionResult> FetchHisXmlBatch([FromBody] HisFetchRequest request,
    CancellationToken ct) => RunHis(async client =>
  {
    if (!ValidBatch(request))
      return BadRequest(new { isError = true, message = "Danh sách MA_LK không hợp lệ (1–500 mã duy nhất)." });
    var xml = await client.Export(request, ct);
    return File(xml, "application/xml", $"XML_{request.Year:D4}{request.Month:D2}.xml");
  });

  [Authorize(Roles = "ADMIN")]
  [HttpGet("HisBrowserScope")]
  public Task<IActionResult> HisBrowserScope() => RunHis(async _ =>
    Ok(new { scope = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
      System.Text.Encoding.UTF8.GetBytes(await HisOwner()))).ToLowerInvariant() }));

  private async Task<string> HisOwner()
  {
    var user = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("USER_NAME")?.Value;
    if (string.IsNullOrWhiteSpace(user)) throw new UnauthorizedAccessException();
    var database = await _dbResolver.GetDatabaseByUserAsync(user);
    if (string.IsNullOrWhiteSpace(database)) throw new InvalidDataException("Không xác định được bệnh viện của tài khoản.");
    return database + "\n" + user;
  }

  private static bool ValidBatch(HisFetchRequest request) => request.MaLks.Length is > 0 and <= 500 &&
    request.MaLks.Distinct().Count() == request.MaLks.Length &&
    request.MaLks.All(id => !string.IsNullOrWhiteSpace(id) && id.Length <= 100 && !id.Contains(',') && !id.Any(char.IsControl));

  [Authorize(Roles = "ADMIN")]
  [HttpPost("FetchHisRecords")]
  public Task<IActionResult> FetchHisRecords([FromBody] HisFetchRequest request, CancellationToken ct) =>
    RunHis(async client => Ok(await client.FetchPage(request, ct)));

  [Authorize(Roles = "ADMIN")]
  [HttpPost("FetchAndImportHisBatch")]
  public Task<IActionResult> FetchAndImportHisBatch([FromBody] HisFetchRequest request, CancellationToken ct) =>
    RunHis(async client =>
    {
      if (request.MaLks.Length == 0 || request.MaLks.Distinct().Count() != request.MaLks.Length ||
          request.MaLks.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Contains(',') || id.Any(char.IsControl)))
        return BadRequest(new { isError = true, message = "Danh sách MA_LK không hợp lệ (1–500 mã duy nhất)." });
      var bytes = await client.Export(request, ct);
      using var stream = new MemoryStream(bytes);
      return await ImportXMLHospitalData(new FormFile(stream, 0, bytes.Length, "file", "HIS.xml"));
    });

  private async Task<IActionResult> RunHis(Func<HisXmlClient, Task<IActionResult>> action)
  {
    Response.Headers.CacheControl = "no-store";
    try
    {
      using var client = new HisXmlClient();
      return await action(client);
    }
    catch (InvalidDataException ex) { return BadRequest(new { isError = true, message = ex.Message }); }
    catch (UnauthorizedAccessException) { return StatusCode(403, new { isError = true, message = "Không có quyền truy cập phiên hoặc kho XML." }); }
    catch (Exception ex) when (ex is JsonException or XmlException or FormatException)
    { return BadRequest(new { isError = true, message = "Dữ liệu HIS không hợp lệ. Kiểm tra token, cookie và định dạng XML." }); }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
    { return StatusCode(502, new { isError = true, message = "Kết nối HIS bị gián đoạn hoặc quá thời gian chờ. Vui lòng thử lại." }); }
  }
}
