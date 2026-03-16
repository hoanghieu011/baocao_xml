using System.ComponentModel.DataAnnotations;

public class NhanVienDto
{
    public ulong id { get; set; }
    public string ten_bo_phan { get; set; }
    public string ma_nv { get; set; }
    public string full_name { get; set; }
    public string vi_tri { get; set; }
    public string role { get; set; }
}
public class SearchNhanVienDto
{
    public ulong id { get; set; }
    public string ma_nv { get; set; }
    public string full_name { get; set; }
    public string gioi_tinh { get; set; }
    public string ten_bo_phan { get; set; }
    public string ma_vi_tri { get; set; }
    public string cong_viec { get; set; }
}
public class UpdateNhanVienDto
{
    public string full_name { get; set; }
    public string gioi_tinh { get; set; }
    public ulong? bo_phan_id { get; set; }
    public string ma_vi_tri { get; set; }
    public string? cong_viec { get; set; }
    public string? email { get; set; }
}
public class NhanVienXuLyDto
{
    public ulong id { get; set; }
    public string ma_nv { get; set; }
    public string full_name { get; set; }
    public ulong? bo_phan_id { get; set; }
    public string? ma_vi_tri { get; set; }
    public string? tenBoPhan { get; set; }
    public string? cong_viec { get; set; }
    public string? email { get; set; }
    public string? vi_tri { get; set; }
}
public class CreatNhanVienResponse
{
    public string ma_nv { get; set; }
    public ulong id { get; set; }
    public string password { get; set; }
}
public class NhanVienDetailsDto
{
    public ulong Id { get; set; }
    public string MaNv { get; set; }
    public string FullName { get; set; }
    public string GioiTinh { get; set; }
    public string CongViec { get; set; }
    public string BoPhan { get; set; }
    public string? MaViTri { get; set; }
    public string ViTri { get; set; }
    public string? Email { get; set; }
    public int? Xoa { get; set; }
    public int? PhepTon { get; set; } = 0;
}
public class NhanVienDuyetDto
{
    public string ma_nv { get; set; }
    public string full_name { get; set; }
    public string ma_vi_tri { get; set; }
    public string ten_bo_phan { get; set; }
    public string cong_viec { get; set; }
    public string email { get; set; }
}


