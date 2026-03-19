using Microsoft.EntityFrameworkCore;

namespace API.Common
{
    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; } = "server=10.30.31.177;port=3306;database=his_common;user=user;password=123456";
        public MySqlServerVersion MySqlVersion { get; set; } = new MySqlServerVersion(new Version(8, 0, 21));
    }
}
