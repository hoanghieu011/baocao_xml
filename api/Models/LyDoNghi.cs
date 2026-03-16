namespace API.Models
{
    public class LyDoNghi
    {
        public ulong id { get; set; }
        public string ky_hieu { get; set; }
        public string dien_giai { get; set; }
        public LyDoNghi(string ky_hieu, string dien_giai)
        {
            this.id = 0;
            this.ky_hieu = ky_hieu;
            this.dien_giai = dien_giai;
        }
        public LyDoNghi() { }
    }
}