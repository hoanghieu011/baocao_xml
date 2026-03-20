using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class XML3
    {
        [Key]
        public ulong XML3ID { get; set; }
        public int? STT { get; set; }
        public string MA_LK { get; set; }
        public string? MA_DICH_VU { get; set; }
        public string? MA_PTTT_QT { get; set; }
        public string? MA_VAT_TU { get; set; }
        public string? MA_NHOM { get; set; }
        public string? GOI_VTYT { get; set; }
        public string? TEN_VAT_TU { get; set; }
        public string? TEN_DICH_VU { get; set; }
        public string? MA_XANG_DAU { get; set; }
        public string? DON_VI_TINH { get; set; }
        public string? PHAM_VI { get; set; }
        public int? SO_LUONG { get; set; }
        public int? DON_GIA_BV { get; set; }
        public int? DON_GIA_BH { get; set; }
        public int? TT_THAU { get; set; }
        public int? TYLE_TT_DV { get; set; }
        public int? TYLE_TT_BH { get; set; }
        public int? THANH_TIEN_BV { get; set; }
        public int? THANH_TIEN_BH { get; set; }
        public int? T_TRANTT { get; set; }
        public int? MUC_HUONG { get; set; }
        public int? T_NGUONKHAC_NSNN { get; set; }
        public int? T_NGUONKHAC_VTNN { get; set; }
        public int? T_NGUONKHAC_VTTN { get; set; }
        public int? T_NGUONKHAC_CL { get; set; }
        public int? T_NGUONKHAC { get; set; }
        public int? T_BNTT { get; set; }
        public int? T_BNCCT { get; set; }
        public int? T_BHTT { get; set; }
        public string? MA_KHOA { get; set; }
        public string? MA_GIUONG { get; set; }
        public string? MA_BAC_SI { get; set; }
        public string? NGUOI_THUC_HIEN { get; set; }
        public string? MA_BENH { get; set; }
        public string? MA_BENH_YHCT { get; set; }
        public DateTime? NGAY_YL { get; set; }
        public DateTime? NGAY_TH_YL { get; set; }
        public DateTime? NGAY_KQ { get; set; }
        public string? MA_PTTT { get; set; }
        public string? VET_THUONG_TP { get; set; }
        public string? PP_VO_CAM { get; set; }
        public string? VI_TRI_TH_DVKT { get; set; }
        public string? MA_MAY { get; set; }
        public string? MA_HIEU_SP { get; set; }
        public string? TAI_SU_DUNG { get; set; }
        public string? DU_PHONG { get; set; }
        public int? CSYTID { get; set; }
    }
}
