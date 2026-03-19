using API.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        /// Chú ý: không dispose connection do DbContext quản lý lifecycle.
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

        public async Task<string?> GetCsytIdByUserAsync(string userName)
        {
            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
               SELECT CSYTID FROM org_officer 
                    WHERE OFFICER_ID = (
                        SELECT OFFICER_ID FROM adm_user 
                        WHERE USER_NAME = @user
                    )";

            var p = cmd.CreateParameter();
            p.ParameterName = "@user";
            p.Value = userName;
            cmd.Parameters.Add(p);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task<int> CheckIfBenhNhanTonTai(string maLK, string dbData)
        {
            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(1) from {dbData} WHERE MA_LK= @p ";

            var p = cmd.CreateParameter();
            p.ParameterName = "@p";
            p.Value = maLK;
            cmd.Parameters.Add(p);

            var sqlres = await cmd.ExecuteScalarAsync();
            if(int.TryParse(sqlres?.ToString(), out int res)) {
                return res;
            }
            return 0;
        }
    }
}