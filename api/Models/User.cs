using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        [Key]
        public ulong USER_ID { get; set; }
        public string? FULL_NAME { get; set; }
        public string USER_NAME { get; set; }
        public string USER_PWD { get; set; }
        public int? OFFICER_ID { get; set; }
        public int? USER_LEVEL { get; set; }
        public int? CSYTID { get; set; }
        public int? STATUS { get; set; }
        public string? NOTE { get; set; }
        public string? ROLE { get; set; }
        public DateTime? CREATED_DATETIME { get; set; }
    }
}
