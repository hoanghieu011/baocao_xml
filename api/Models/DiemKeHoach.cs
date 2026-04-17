using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class DiemKeHoach
    {
        [Column("DIEMKEHOACHID")]
        public int? diemKeHoachId  { get; set; }
        [Column("KHOA")]
        public string khoa { get; set; }
        [Column("OFFICER_NAME")]
        public string officerName { get; set; }
        [Column("BACSIID")]
        public int? bacSiId  { get; set; }
        [Column("DIEM_KEHOACH")]
        public int? diemKeHoach  { get; set; }
        [Column("SO_BUOITRUC")]
        public int? soBuoiTruc  { get; set; }
        [Column("SO_BENHNHAN")]
        public int? soBenhNhan  { get; set; }
        [Column("DIEM_TRUC")]
        public int? diemTruc  { get; set; }
        [Column("DIEM_TRUC_CC")]
        public int? diemTrucCC  { get; set; }
        [Column("DIEM_LAYMAU")]
        public int? diemLayMau  { get; set; }
        [Column("THANGNAM")]
        public int? thangNam  { get; set; }
        [Column("OFFICER_TYPE")]
        public int? officerType  { get; set; }
        //[Column("NGAYTAO")]
        //public DateTime? ngayTao { get; set; }
    }
}