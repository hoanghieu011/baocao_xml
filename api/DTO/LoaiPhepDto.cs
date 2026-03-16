public class LoaiPhepDto
{
    public string ten_loai_phep { get; set; }
    public int so_ngay_nghi { get; set; }
    public LoaiPhepDto (string ten_loai_phep, int so_ngay_nghi){
        this.ten_loai_phep = ten_loai_phep;
        this.so_ngay_nghi = so_ngay_nghi;
    }
}