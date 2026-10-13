using System.ComponentModel.DataAnnotations;

namespace api.DTO;

public class HisFetchRequest
{
  [Range(2000, 9999)] public int Year { get; set; }
  [Range(1, 12)] public int Month { get; set; }
  [Required, StringLength(16384)] public string Token { get; set; } = "";
  [Required, StringLength(16384), RegularExpression(@"^[^\r\n]+$")]
  public string Cookie { get; set; } = "";
  [Required, RegularExpression(@"^\d{1,10}$")] public string Cskcb { get; set; } = "35147";
  public string Dtbnid => "100";
  public string LoaiXml => "130";
  [Required, RegularExpression("^[0-5]$")] public string LocTheo { get; set; } = "2";
  [Range(1, 100000)] public int Page { get; set; } = 1;
  [Range(1, 500)] public int Rows { get; set; } = 500;
  [Required, RegularExpression("^(auto|0|1)$")] public string Qd3176 { get; set; } = "auto";
  [Required, MaxLength(500)] public string[] MaLks { get; set; } = [];
}

public class HisSessionRequest
{
  [Required, StringLength(16384)] public string Token { get; set; } = "";
  [Required, StringLength(16384), RegularExpression(@"^[^\r\n]+$")]
  public string Cookie { get; set; } = "";
}

public record HisSessionStatus(bool IsValid, string Message);

public record HisRecord(string MaLk, string Qd3176);
public record HisPage(int Total, List<HisRecord> Records);
