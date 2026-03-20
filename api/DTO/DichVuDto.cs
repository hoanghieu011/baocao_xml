using System.ComponentModel.DataAnnotations;
namespace API.DTO
{
    public class DichVuDto
    {
        public int dichvuid { get; set; }
        public string? ma_dichvu { get; set; }
        public string ten_dichvu { get; set; }
        // public int? nhom_dichvuid { get; set; }
        public int? nhom_mabhyt_id { get; set; }
        public string? donvi { get; set; }
        public int? gia_bhyt { get; set; }
        public int? csytid { get; set; }
        // public int? trangthai { get; set; }
        // public DateTime? ngaytao { get; set; }
        public string? tennhom { get; set; }
        public double? chiphi { get; set; }
        public double? heso { get; set; }
    }
}