using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTO
{
    public class NghiPhepDto
    {
        public int so_ngay_nghi { get; set; } = 0;
        public string ky_hieu_ly_do { get; set; }
        public string ly_do_nghi_str { get; set; }
        public string? ban_giao { get; set; }
        public ulong loai_phep_id { get; set; }
        public DateTime nghi_tu { get; set; }
        public DateTime nghi_den { get; set; }
        public int nghi_t7 { get; set; }
        public string? image_name { get; set; }
        public string? image_url { get; set; }
    }
    public class SearchNhanVienXuLyDto
    {
        public string? bo_phan { get; set; }
        public string trang_thai { get; set; }
        public string ma_vi_tri { get; set; }
        public string? cong_viec { get; set; }
    }
    public class NghiPhepSearchDto
    {
        public string? trang_thai { get; set; }
        public string? searchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class NghiPhepResultDto
    {
        public int? Tier { get; set; } = 0;
        public ulong Id { get; set; }
        public int SoNgayNghi { get; set; }
        public string? BanGiao { get; set; }
        public string TrangThai { get; set; }
        public string? NvXuLy1 { get; set; }
        public string? NvXuLy2 { get; set; }
        public string? NvXuLy3 { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayXuLy3 { get; set; }
        public DateTime? NgayXuLy2 { get; set; }
        public DateTime? NgayXuLy1 { get; set; }

        public ulong? LoaiPhepId { get; set; }
        public DateTime? NghiTu { get; set; }
        public DateTime? NghiDen { get; set; }
        public string? NgayNghi { get; set; }
        public string KyHieuLyDo { get; set; }
        public string LyDoDienGiai { get; set; }
        public string LyDoNghiStr { get; set; }
        public int Duyet { get; set; }
        public string MaNv { get; set; }
        public string FullName { get; set; }
        public string TenBoPhan { get; set; }
        public string TenViTri { get; set; }
        public string? CongViec { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? MaNvHuy { get; set; }
        public string? LyDoHuy { get; set; }
        public DateTime? NgayHuy { get; set; }
        public int? ThongBao { get; set; }
        public int? PhepTon { get; set; } = 0;
        public string? ImageName { get; set; }
        public string? ImageUrl { get; set; }

    }
    public class PhieuNghiFilter
    {
        public string? TrangThaiPhieu { get; set; }
        public List<string> TenBoPhanPhieu { get; set; }
        public List<string> MaViTriTaoPhieu { get; set; }
        public string CongViecNvTaoPhieu { get; set; }
        public PhieuNghiFilter()
        {
            TenBoPhanPhieu = new List<string>();
            MaViTriTaoPhieu = new List<string>();
        }
    }
    public class NhanVienXuLyFilter
    {
        public string? TenBoPhan { get; set; }
        public List<string> MaViTri { get; set; }
        public string? CongViec { get; set; }
    }

}
