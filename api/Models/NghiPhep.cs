using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Models
{
    public class NghiPhep
    {
        public ulong id { get; set; }
        public int so_ngay_nghi { get; set; }
        public string ky_hieu_ly_do { get; set; }
        public string ly_do_nghi_str { get; set; }
        public string? trang_thai { get; set; }
        public string? nv_xu_ly_1 { get; set; }
        public string? nv_xu_ly_2 { get; set; }
        public string? nv_xu_ly_3 { get; set; }
        public DateTime ngay_tao { get; set; }
        public DateTime? ngay_xu_ly_1 { get; set; }
        public DateTime? ngay_xu_ly_2 { get; set; }
        public DateTime? ngay_xu_ly_3 { get; set; }
        public ulong loai_phep_id { get; set; }
        public DateTime nghi_tu { get; set; }
        public DateTime nghi_den { get; set; }
        [Column(TypeName = "TEXT")]
        public string? ngay_nghi { get; set; }
        public string? ban_giao { get; set; }
        public int duyet { get; set; }
        public string? ly_do_tu_choi { get; set; }
        public int thong_bao { get; set; }
        public string ma_nv { get; set; }
        public string? nv_huy { get; set; }
        public string? ly_do_huy { get; set; }
        public DateTime? ngay_huy { get; set; }
        public string? image_name { get; set; }
        public string? image_url { get; set; }
    }
}