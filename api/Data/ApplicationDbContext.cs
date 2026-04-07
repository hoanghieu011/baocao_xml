using Microsoft.EntityFrameworkCore;
using API.Models;
using API.DTO;
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
        public DbSet<XML2> xml2 { get; set; }
        public DbSet<XML3> xml3 { get; set; }
        public DbSet<DichVu> dmc_dichvu { get; set; }
        public DbSet<LoaiDichVu> dmc_nhom_mabhyt { get; set; }
        public DbSet<DichVuDto> dto_dichvu { get; set; }
        public DbSet<BcDoanhThuBscdDto> dto_bc_doanhthu_bscd { get; set; }
        public DbSet<BcDoanhThuKhoaDto> dto_bc_doanhthu_khoa { get; set; }
        public DbSet<BcDoanhThuToanVienDto> dto_bc_doanhthu_toanvien { get; set; }
        public DbSet<Officer> org_officer { get; set; }
        public DbSet<Organization> org_organization { get; set; }
        public DbSet<BenhVien> dmc_benhvien { get; set; }
        public DbSet<DiemKeHoach> diemkehoach { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DichVuDto>().HasNoKey();
            modelBuilder.Entity<BcDoanhThuBscdDto>().HasNoKey();
            modelBuilder.Entity<BcDoanhThuKhoaDto>().HasNoKey();
            modelBuilder.Entity<BcDoanhThuToanVienDto>().HasNoKey();
        }
    }

}
