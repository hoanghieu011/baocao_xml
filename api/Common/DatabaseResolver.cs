using System.Data;
using System.Threading.Tasks;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Common
{
    public class DatabaseResolver
    {
        private readonly ApplicationDbContext _context;

        public DatabaseResolver(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trả về tên database (DB_DATA) tương ứng với userName.
        /// </summary>
        public async Task<string?> GetDatabaseByUserAsync(string userName)
        {
            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DB_DATA FROM dmc_benhvien 
                WHERE CSYTID = (
                    SELECT CSYTID FROM org_officer 
                    WHERE OFFICER_ID = (
                        SELECT OFFICER_ID FROM adm_user 
                        WHERE USER_NAME = @user
                    )
                )";

            var p = cmd.CreateParameter();
            p.ParameterName = "@user";
            p.Value = userName;
            cmd.Parameters.Add(p);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
    }
}