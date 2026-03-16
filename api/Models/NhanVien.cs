using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class NhanVien
    {
        public ulong id { get; set; }
        public string ma_nv { get; set; }
        public string full_name { get; set; }
        public string gioi_tinh { get; set; }
        public string ma_vi_tri { get; set; }
        public ulong? bo_phan_id { get; set; }
        public string? cong_viec { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? email { get; set; }
        public int? xoa { get; set; } = 0;
    }
}