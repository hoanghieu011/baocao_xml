namespace API.Models
{
    public class LoaiPhep
    {
        public ulong id { get; set; }
        public string ten_loai_phep { get; set; }
        public int so_ngay_nghi { get; set; }
        public LoaiPhep(string ten_loai_phep, int so_ngay_nghi)
        {
            this.id = 0;
            this.ten_loai_phep = ten_loai_phep;
            this.so_ngay_nghi = so_ngay_nghi;
        }
        public LoaiPhep(ulong id, string ten_loai_phep, int so_ngay_nghi)
        {
            this.id = id;
            this.ten_loai_phep = ten_loai_phep;
            this.so_ngay_nghi = so_ngay_nghi;
        }
    }
}