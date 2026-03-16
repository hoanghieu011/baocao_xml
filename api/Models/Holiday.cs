namespace API.Models
{
    public class Holiday
    {
        public ulong id { get; set; }
        public int year { get; set; }
        public DateTime ngay_nghi { get; set; }
        public string? mo_ta { get; set; }
    }
}