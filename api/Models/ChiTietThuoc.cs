using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    [Table("xml2")]
    public class ChiTietThuoc
    {
        [Key]
        public int XML2ID { get; set; }
        public int? STT { get; set; }
        public string MA_LK { get; set; }
        public string? MA_THUOC { get; set; }
        public string? MA_PP_CHEBIEN { get; set; }
        public string? MA_CSKCB_THUOC { get; set; }
        public string? MA_NHOM { get; set; }
        public string? TEN_THUOC { get; set; }
        public string? DON_VI_TINH { get; set; }
        public string? HAM_LUONG { get; set; }
        public string? DUONG_DUNG { get; set; }
        public string? DANG_BAO_CHE { get; set; }
        public string? LIEU_DUNG { get; set; }
        public string? CACH_DUNG { get; set; }
        public string? SO_DANG_KY { get; set; }
        public string? TT_THAU { get; set; }
        public string? PHAM_VI { get; set; }
        public int? TYLE_TT_BH { get; set; }
        public int? SO_LUONG { get; set; }
        public int? DON_GIA { get; set; }
        public int? THANH_TIEN_BV { get; set; }
        public int? THANH_TIEN_BH { get; set; }
        public int? T_NGUONKHAC_NSNN { get; set; }
        public int? T_NGUONKHAC_VTNN { get; set; }
        public int? T_NGUONKHAC_VTTN { get; set; }
        public int? T_NGUONKHAC_CL { get; set; }
        public int? T_NGUONKHAC { get; set; }
        public int? MUC_HUONG { get; set; }
        public int? T_BNTT { get; set; }
        public int? T_BNCCT { get; set; }
        public int? T_BHTT { get; set; }
        public string? MA_KHOA { get; set; }
        public string? MA_BAC_SI { get; set; }
        public string? MA_DICH_VU { get; set; }
        public DateTime? NGAY_YL { get; set; }
        public DateTime? NGAY_TH_YL { get; set; }
        public string? MA_PTTT { get; set; }
        public string? NGUON_CTRA { get; set; }
        public string? VET_THUONG_TP { get; set; }
        public string? DU_PHONG { get; set; }
        public int? CSYTID { get; set; }
        public DateTime VERSION { get; set; }
    }
}
