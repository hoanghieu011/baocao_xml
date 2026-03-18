using Microsoft.EntityFrameworkCore;
using API.Models;
using api.Models;

namespace API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> adm_user { get; set; }
        public DbSet<XML1> xml1 { get; set; }
        // public DbSet<NhanVien> nhan_vien { get; set; }
        // public DbSet<NghiPhep> nghi_phep { get; set; }
        // public DbSet<LoaiPhep> loai_phep { get; set; }
        // public DbSet<BoPhan> bo_phan { get; set; }
        // public DbSet<ViTri> vi_tri { get; set; }
        // public DbSet<LyDoNghi> ly_do_nghi { get; set; }
        // public DbSet<PhepTon> phep_ton { get; set; }
        // public DbSet<Holiday> holiday { get; set; }
        public DbSet<XML1> thong_tin_benh_nhan { get; set; }
        public DbSet<XML2> chi_tiet_thuoc { get; set; }
        public DbSet<XML3> dich_vu_ki_thuat { get; set; }
    }
}
