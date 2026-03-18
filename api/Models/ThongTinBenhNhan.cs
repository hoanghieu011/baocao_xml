using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    [Table("xml1")]
    public class ThongTinBenhNhan
    {
        [Key]
        public int XML1ID { get; set; }
        public int STT { get; set; }
        public string MA_LK { get; set; }
        public string? MA_BN { get; set; }
        public string? HO_TEN { get; set; }
        public string? SO_CCCD { get; set; }
        public string? NGAY_SINH { get; set; }
        public int? GIOI_TINH { get; set; }
        public string? NHOM_MAU { get; set; }
        public string? MA_QUOCTICH { get; set; }
        public string? MA_DANTOC { get; set; }
        public string? MA_NGHE_NGHIEP { get; set; }
        public string? DIA_CHI { get; set; }
        public string? MATINH_CU_TRU { get; set; }
        public string? MAHUYEN_CU_TRU { get; set; }
        public string? MAXA_CU_TRU { get; set; }
        public string? DIEN_THOAI { get; set; }
        public string? MA_THE_BHYT { get; set; }
        public string? MA_DKBD { get; set; }
        public string? GT_THE_TU { get; set; }
        public string? GT_THE_DEN { get; set; }
        public string? NGAY_MIEN_CCT { get; set; }
        public string? LY_DO_VV { get; set; }
        public string? LY_DO_VNT { get; set; }
        public string? MA_LY_DO_VNT { get; set; }
        public string? CHAN_DOAN_VAO { get; set; }
        public string? CHAN_DOAN_RV { get; set; }
        public string? MA_BENH_CHINH { get; set; }
        public string? MA_BENH_KT { get; set; }
        public string? MA_BENH_YHCT { get; set; }
        public string? MA_PTTT_QT { get; set; }
        public string? MA_DOITUONG_KCB { get; set; }
        public string? MA_NOI_DI { get; set; }
        public string? MA_NOI_DEN { get; set; }
        public string? MA_TAI_NAN { get; set; }
        public DateTime? NGAY_VAO { get; set; }
        public DateTime? NGAY_VAO_NOI_TRU { get; set; }
        public DateTime NGAY_RA { get; set; }
        public string? GIAY_CHUYEN_TUYEN { get; set; }
        public string? SO_NGAY_DTRI { get; set; }
        public string? PP_DIEU_TRI { get; set; }
        public string? KET_QUA_DTRI { get; set; }
        public string? MA_LOAI_RV { get; set; }
        public string? GHI_CHU { get; set; }
        public DateTime? NGAY_TTOAN { get; set; }
        public int? T_THUOC { get; set; }
        public int? T_VTYT { get; set; }
        public int? T_TONGCHI_BV { get; set; }
        public int? T_TONGCHI_BH { get; set; }
        public int? T_BNTT { get; set; }
        public int? T_BNCCT { get; set; }
        public int? T_BHTT { get; set; }
        public int? T_NGUONKHAC { get; set; }
        public int? T_BHTT_GDV { get; set; }
        public string? NAM_QT { get; set; }
        public string? THANG_QT { get; set; }
        public string? MA_LOAI_KCB { get; set; }
        public string? MA_KHOA { get; set; }
        public string? MA_CSKCB { get; set; }
        public string? MA_KHUVUC { get; set; }
        public string? CAN_NANG { get; set; }
        public string? CAN_NANG_CON { get; set; }
        public int? NAM_NAM_LIEN_TUC { get; set; }
        public string? NGAY_TAI_KHAM { get; set; }
        public string? MA_HSBA { get; set; }
        public string? MA_TTDV { get; set; }
        public string? DU_PHONG { get; set; }
        public int? CSYTID { get; set; }
        public DateTime VERSION { get; set; }
    }
}
