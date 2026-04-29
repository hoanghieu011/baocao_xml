using Org.BouncyCastle.Asn1.Mozilla;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    public class DiemCtkh
    {
        public int KhoaId { get; set; }
        public string Khoa { get; set; }
        /// <summary>
        /// Tên bác sĩ, điều dưỡng
        /// </summary>
        [Column("OFFICER_NAME")]
        public string OfficerName { get; set; } 
        [Column("OFFICER_TYPE")]
        public int OfficerType { get; set; } = 0;
        [Column("DIEM_KEHOACH")]
        public decimal? DiemKeHoach { get; set; } = 0;
        /// <summary>
        /// Điểm Chỉ định khám, điều trị phát thuốc ( Dược )
        /// </summary>
        [Column("DIEMTHUCHIEN")]
        public decimal? DiemCdKham { get; set; } = 0;
        /// <summary>
        /// Điểm CĐ nhập viện, Dược (hc)
        /// </summary>
        [Column("DIEMCDNHAPVIEN")]
        public decimal? DiemCDDieuTri { get; set; } = 0;
        /// <summary>
        /// Điểm phẫu, thủ thuật : chỉ định
        /// </summary>
        [Column("DIEMPTTCHIDINH")]
        public decimal? DiemPTTCD { get; set; } = 0;
        /// <summary>
        /// Điểm phẫu, thủ thuật : thực hiện
        /// </summary>
        [Column("DIEMPTTTHUCHIEN")]
        public decimal? DiemPTTTH { get; set; } = 0;
        /// <summary>
        ///  Điểm BH/ĐT Tcường
        /// </summary>
        [Column("DIEMTANGCUONG")]
        public decimal? DiemTangCuong { get; set; } = 0;
        [Column("SONGAYTANGCUONG")]
        public decimal? SoNgayTangCuong { get; set; } = 0;
        /// <summary>
        /// Điểm trực
        /// </summary>
        [Column("DIEM_TRUC")]
        public decimal? DiemTruc { get; set; } = 0;
        /// <summary>
        ///  Điểm cộng BA ngoại trú; TH siêu âm, Dược (tổng số lượt khám có đơn thuốc > 15 ngày)
        /// </summary>
        [Column("DIEMBANT")]
        public decimal? DiemCongBANT { get; set; } = 0;
        /// <summary>
        /// Điểm TH PTT theo điều dưỡng
        /// </summary>
        //public decimal DiemTHPTTTheoDD { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Chỉ định
        /// </summary>
        //public decimal DiemBNNDCD { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Thực hiện
        /// </summary>
        //public decimal DiemBNNDTH { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Chỉ định Nhập viện
        /// </summary>
        //public decimal DiemBNNDCDNhapVien { get; set; } = 0;
    }
}
