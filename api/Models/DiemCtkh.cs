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
        public int? DiemKeHoach { get; set; } = 0;
        /// <summary>
        /// Điểm Chỉ định khám, điều trị phát thuốc ( Dược )
        /// </summary>
        [Column("DIEMTHUCHIEN")]
        public float? DiemCdKham { get; set; } = 0;
        /// <summary>
        /// Điểm CĐ nhập viện, Dược (hc)
        /// </summary>
        [Column("DIEMCDNHAPVIEN")]
        public float? DiemCDDieuTri { get; set; } = 0;
        /// <summary>
        /// Điểm phẫu, thủ thuật : chỉ định
        /// </summary>
        [Column("DIEMPTTCHIDINH")]
        public float? DiemPTTCD { get; set; } = 0;
        /// <summary>
        /// Điểm phẫu, thủ thuật : thực hiện
        /// </summary>
        [Column("DIEMPTTTHUCHIEN")]
        public float? DiemPTTTH { get; set; } = 0;
        /// <summary>
        ///  Điểm BH/ĐT Tcường
        /// </summary>
        [Column("DIEMTANGCUONG")]
        public int? DiemTangCuong { get; set; } = 0;
        /// <summary>
        /// Điểm trực
        /// </summary>
        [Column("DIEM_TRUC")]
        public int? DiemTruc { get; set; } = 0;
        /// <summary>
        ///  Điểm cộng BA ngoại trú; TH siêu âm, Dược (tổng số lượt khám có đơn thuốc > 15 ngày)
        /// </summary>
        //public float DiemCongBANT { get; set; } = 0;
        /// <summary>
        /// Điểm TH PTT theo điều dưỡng
        /// </summary>
        //public float DiemTHPTTTheoDD { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Chỉ định
        /// </summary>
        //public float DiemBNNDCD { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Thực hiện
        /// </summary>
        //public float DiemBNNDTH { get; set; } = 0;
        /// <summary>
        /// Điểm BNND Chỉ định Nhập viện
        /// </summary>
        //public float DiemBNNDCDNhapVien { get; set; } = 0;
    }
}
