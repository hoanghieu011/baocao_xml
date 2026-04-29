using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Officer
    {
        [Key]
        public int officer_id { get; set; }
        public string officer_code { get; set; }
        public string officer_name { get; set; }
        public DateTime? ngaysinh { get; set; }
        public int? gioitinh { get; set; }
        //public string? diachi { get; set; }
        //public string? phone { get; set; }
        //public int? cccd { get; set; }
        //public string? email { get; set; }
        //public string? chucdanh { get; set; }
        public int? status { get; set; }
        public int? csytid { get; set; }
        public DateTime? created_datetime { get; set; }
        public int? officer_type { get; set; }
        public int? bacsiid { get; set; }
        //public string? hoc_ham { get; set; }
        //public string? hoc_vi { get; set; }
        //public string? chuc_danh { get; set; }
        public string? ma_bac_si { get; set; }
        public int? khoaid { get; set; }
    }
}