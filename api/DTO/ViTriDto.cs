public class ViTriDto
{
    public string ma_vi_tri { get; set; }
    public string ten_vi_tri { get; set; }
    public string ten_vi_tri_en { get; set; }
    public ViTriDto(string ma_vi_tri, string ten_vi_tri, string ten_vi_tri_en)
    {
        this.ma_vi_tri = ma_vi_tri;
        this.ten_vi_tri = ten_vi_tri;
        this.ten_vi_tri_en = ten_vi_tri_en;
    }
}
public class UpdateViTriDto
{
    public string ten_vi_tri { get; set; }
    public string ten_vi_tri_en { get; set; }
}