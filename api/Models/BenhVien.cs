using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class BenhVien
    {
        [Key]
        public int benhvienid { get; set; }
        public string? benhvienkcbbd { get; set; }
        public string mabenhvien { get; set; }
        public string tenbenhvien { get; set; }
        public string? diachi { get; set; }
        public string? matinh { get; set; }
        public string? mahuyen { get; set; }
        public string? maxa { get; set; }
        public int? status { get; set; }
        public int? csytid { get; set; }
        public DateTime? created_datetime { get; set; }
        public string? db_data { get; set; }
    }
}