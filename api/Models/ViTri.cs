namespace API.Models
{
    public class ViTri
    {
        public ulong id { get; set; }
        public string ma_vi_tri { get; set; }
        public string ten_vi_tri { get; set; }
        public string ten_vi_tri_en { get; set; }
        public ViTri(string ma_vi_tri, string ten_vi_tri, string ten_vi_tri_en)
        {
            this.id = 0;
            this.ma_vi_tri = ma_vi_tri;
            this.ten_vi_tri = ten_vi_tri;
            this.ten_vi_tri_en = ten_vi_tri_en;
        }
        public ViTri() { }
    }
}