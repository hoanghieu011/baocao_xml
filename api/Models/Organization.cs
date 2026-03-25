using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Organization
    {
        [Key]
        public int org_id { get; set; }
        public int? parent_id { get; set; }
        public string? org_code { get; set; }
        public string? org_name { get; set; }
        public int? org_level { get; set; }
        public int? status { get; set; }
        public DateTime? ngaytao { get; set; }
        public string? ma_khoa { get; set; }
        public int csytid { get; set; }
    }
}
