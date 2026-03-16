namespace API.Models
{
    public class BoPhan
    {
        public ulong id { get; set; }
        public string ten_bo_phan { get; set; }
        public BoPhan(string ten_bo_phan)
        {
            this.id = 0;
            this.ten_bo_phan = ten_bo_phan;
        }
    }
}