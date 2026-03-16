using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        public ulong id { get; set; }
        public ulong id_nv { get; set; }
        public string ma_nv { get; set; }
        public string password { get; set; }
        public string? role { get; set; }
    }
}
