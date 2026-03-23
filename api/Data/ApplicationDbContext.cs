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
        public DbSet<DiemKeHoach> diemkehoach { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DichVuDto>().HasNoKey();
        }
    }

}
