namespace API.Models
{
    /// <summary>
    /// Danh mục ICD-10 (dùng chung, lưu ở his_common.dmc_icd10).
    /// Dữ liệu được nạp từ nguồn Bộ Y tế (icd.kcb.vn).
    /// </summary>
    public class Icd10
    {
        public int icd10id { get; set; }
        // Mã ICD hiển thị (vd: A00, A00.0, A00-B99)
        public string? ma_icd { get; set; }
        // Mã định danh node từ nguồn, duy nhất (vd: A000). Dùng để đồng bộ (upsert).
        public string ma_id { get; set; } = "";
        // Tên bệnh / nhóm bệnh (tiếng Việt)
        public string? ten_icd { get; set; }
        // Loại node trong cây: chapter / section / type / disease
        public string? loai { get; set; }
        // Mã định danh node cha
        public string? ma_cha { get; set; }
        // 1 = node lá (mã bệnh cụ thể), 0 = node nhóm/chương (còn con)
        public int? is_leaf { get; set; }
        // Cấp trong cây (1 = chương)
        public int? cap { get; set; }
        public int? trangthai { get; set; }
        public DateTime? ngaytao { get; set; }
        public string? nguon { get; set; }
    }
}
