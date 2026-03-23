using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class BC_DIEMKEHOACH
    {
        public int DIEMKEHOACHID  { get; set; } 
        public int? BACSIID  { get; set; } 
        public int? DIEM_KEHOACH  { get; set; } 
        public int? SO_BUOITRUC  { get; set; } 
        public int? SO_BENHNHAN  { get; set; } 
        public int? DIEM_TRUC  { get; set; } 
        public int? DIEM_TRUC_CC  { get; set; } 
        public int? DIEM_LAYMAU  { get; set; } 
        public int? THANGNAM  { get; set; } 
    }
}