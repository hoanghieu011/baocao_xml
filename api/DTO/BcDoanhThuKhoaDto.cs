namespace API.DTO
{
    public class BcDoanhThuKhoaDto
    {
        public int? nhom_mabhyt_id { get; set; }
        public string? ma_dich_vu { get; set; }
        public string? ma_khoa { get; set; }
        public string? khoa { get; set; }
        public string? ten_dich_vu { get; set; }
        public string? tennhom { get; set; }
        public string? ten_bacsi { get; set; }
        public int? don_gia_bh { get; set; }
        public decimal? heso { get; set; }
        public decimal? chiphi { get; set; }
        public decimal? soluong { get; set; }
        public decimal? chiphi_vattu { get; set; }
        //public decimal? thanh_tien { get; set; }
        //public decimal? sotien_conlai { get; set; }
        public decimal? diem_thuchien { get; set; }
    }
}
