using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class KBH_DIEMKEHOACH
    {
        public int ID  { get; set; } 
        public int? BACSIID  { get; set; } 
        public int? DIEM_KEHOACH  { get; set; } 
        public int? SO_BUOITRUC  { get; set; } 
        public int? SO_BENHNHAN  { get; set; } 
        public int? DIEM_TRUC  { get; set; } 
        public int? DIEM_TRUC_CC  { get; set; } 
        public int? DIEM_LAYMAU  { get; set; } 
        public int? THANG  { get; set; } 
        public int? NAM  { get; set; } 
    }
}