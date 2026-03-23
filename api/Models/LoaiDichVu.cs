using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class LoaiDichVu
    {
        [Key]
        public int nhom_mabhyt_id { get; set; }
        public string tennhom { get; set; }
        public string? ghichu { get; set; }
        public string? manhom_bhyt { get; set; }
        public string? nhomid_cha { get; set; }
    }
}