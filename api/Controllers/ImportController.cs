using api.Models;
using API.Common;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using OfficeOpenXml;
using Org.BouncyCastle.Utilities;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Xml.Linq;
using Telegram.BotAPI.AvailableTypes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DatabaseResolver _dbResolver;
        public ImportController(ApplicationDbContext dbContext, DatabaseResolver dbResolver)
        {
            _dbContext = dbContext;
            _dbResolver = dbResolver;
        }
        [HttpPost("test")]
        public async Task<IActionResult> TestUnderlyingType()
        {
            Type t = typeof(XML3);
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var res = "";
            foreach(var p in props)
            {
                Type propertyType = p.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType);
                var pTypeName = underlyingType!=null ? underlyingType.Name : propertyType.Name;
                res += "\n";
                res += $"{p.Name}:{pTypeName}";
            }
            return Ok(res);
        }
        
        [Authorize]
        [HttpPost("ImportHospitalData")]
        public async Task<IActionResult> ImportHospitalData(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return BadRequest("File rỗng hoặc không hợp lệ.");
            }
            if(!file.FileName.EndsWith(".xml"))
            {
                return BadRequest("Vui lòng Upload file .xml!");
            }
            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

            if (string.IsNullOrEmpty(userName))
                return Unauthorized();

            // Lấy tên database động thông qua service dùng chung
            var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
            if (string.IsNullOrEmpty(dbData))
                return BadRequest("Không xác định được database dữ liệu cho user.");
            // Lấy csyt Id động thông qua service dùng chung
            var tempCsytId = await _dbResolver.GetCsytIdByUserAsync(userName);
            if (string.IsNullOrEmpty(tempCsytId))
                return BadRequest("Không xác định được csyt cho user.");
            var csytId = 0;
            if(int.TryParse(tempCsytId, out int value))
            {
                csytId = value;
            }
            // Validate identifier (chỉ cho phép chữ, số, underscore)
            if (!Regex.IsMatch(dbData, @"^[A-Za-z0-9_]+$"))
                return BadRequest("Tên database không hợp lệ.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, Async = true };
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = XmlReader.Create(stream, settings);
                var msg = "";
                // đọc xml
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element || reader.Name != "HOSO")
                        continue;
                    var hosoEl = XElement.ReadFrom(reader) as XElement;
                    if (hosoEl == null) continue;

                    var bn = new XML1();
                    var dsChiTietThuoc = new List<XML2>();
                    var dsDichVuKiThuat = new List<XML3>();

                    ResultInfo resThemBn = null;
                    ResultInfo resThemDsThuoc = null;
                    ResultInfo resThemDsDvkt = null;
                    var maLK = "";
                    foreach (var fileHNode in hosoEl.Elements("FILEHOSO"))
                    {
                        var loai = (string?)fileHNode.Element("LOAIHOSO") ?? "";
                        var noidungXml = fileHNode.Element("NOIDUNGFILE");
                        var encodedContent = noidungXml.Value;
                        byte[] decoded = Convert.FromBase64String(encodedContent);
                        string xml = Encoding.UTF8.GetString(decoded);
                        XElement noidung = XElement.Parse(xml);
                        if (loai.Equals("XML1"))
                        {
                            if (noidung == null) continue;
                            maLK = (string?)noidung.Element("MA_LK") ?? "";
                            if (string.IsNullOrWhiteSpace(maLK)) continue;
                            var exists = await _dbResolver.CheckIfBenhNhanTonTai(maLK, $"`{dbData}`.xml1");
                            if (exists != 0) continue;

                            // thông tin bệnh nhân
                            resThemBn = await ThemBenhNhan(noidung, $"`{dbData}`.xml1", csytId);
                        }
                        else if (loai.Equals("XML2"))
                        {
                            var chiTietThuocXmWrapper = noidung.Element("DSACH_CHI_TIET_THUOC");
                            var dsChiTietThuocXml = chiTietThuocXmWrapper.Elements("CHI_TIET_THUOC");
                            resThemDsThuoc = await ThemChiTietThuoc(dsChiTietThuocXml, $"`{dbData}`.xml2", csytId);
                        }
                        else if (loai.Equals("XML3"))
                        {
                            var chiTietDvktXmWrapper = noidung.Element("DSACH_CHI_TIET_DVKT");
                            var dsChiTietDvktXml = chiTietDvktXmWrapper.Elements("CHI_TIET_DVKT");
                            resThemDsThuoc = await ThemDvkt(dsChiTietDvktXml, $"`{dbData}`.xml3", csytId);
                        }
                    }
                    var flag = 0;
                    if (resThemBn != null && resThemBn.status_code != 200)
                    {
                        flag = 1;
                        msg += resThemBn.message + "\n";
                    }
                    if (resThemDsDvkt != null && resThemDsDvkt.status_code != 200)
                    {
                        flag = 1;
                        msg += resThemDsDvkt.message + "\n";
                    }
                    if (resThemDsThuoc != null && resThemDsThuoc.status_code != 200)
                    {
                        flag = 1;
                        msg += resThemDsThuoc.message + "\n";
                    }
                    if (flag == 1)
                    {
                        if (maLK != "")
                        {
                            // xoá dữ liệu với mã lk đang bị lỗi;
                            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM $`{dbData}`.xml1 WHERE MA_LK='{maLK}'");
                            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM $`{dbData}`.xml2 WHERE MA_LK='{maLK}'");
                            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM $`{dbData}`.xml3 WHERE MA_LK='{maLK}'");
                        }
                        return StatusCode(500, $"Lỗi SQL: ở {maLK} : {msg}");
                    }
                }
                msg = "Thêm mới thành công!";
                return Ok(msg);
            }
            catch (XmlException xe)
            {
                return BadRequest($"Lỗi XML: " + xe.Message);
            }
            catch (FormatException fe)
            {
                return BadRequest($"Lỗi Base64: " + fe.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
            
        }

        static string ConvertCompactTimestampToStr(long compactTimestamp, string formatStr= "HH:mm:ss dd-MM-yyyy")
            {
                if (compactTimestamp == 0) return "";
                string s = compactTimestamp.ToString().PadLeft(12, '0');

                const string format = "yyyyMMddHHmm";
                if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {

                    var res =  dt.ToString(formatStr, CultureInfo.InvariantCulture);
                return res;
                }

                throw new FormatException($" '{compactTimestamp}' không đúng định dạng '{format}'.");
            }

        static DateTime ConvertCompactTimestampToDateTime(long compactTimestamp)
            {
                string s = compactTimestamp.ToString().PadLeft(12, '0');

                const string format = "yyyyMMddHHmm";

                if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {
                    //  'HH:mm:ss dd-MM-yyyy'
                    return dt;
                }

                throw new FormatException($" '{compactTimestamp}' không đúng định dạng '{format}'.");
            }
        static string ReplaceCData(string inp)
        {
            var res = inp.Replace("<![CDATA[", "").Replace("]]>", "").Replace("\\", "").Replace("'", "''");
            return res;
        }

        static int GetInt(XElement? e, int defaultVal = 0) { 
            if (e == null) return defaultVal; 
            if (int.TryParse(e.Value.Trim(), out var v)) return v; 
            return defaultVal;
            throw new Exception("GetInt exception: " + e);
        }

        static long GetLong(XElement? e, int defaultVal = 0)
        {
            if (e == null) return defaultVal;
            if (long.TryParse(e.Value.Trim(), out var v)) return v;
            return defaultVal;
            throw new Exception("GetLong exception: " + e);
        }

        async Task<ResultInfo> ThemBenhNhan(XElement xmlData, string table, int csytid)
        {
            Type t = typeof(XML1);
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return await GenerateSqlAddSingleObj(xmlData, props, table,csytid);
        }

        async Task<ResultInfo> ThemChiTietThuoc(IEnumerable<XElement> xmlData, string table, int csytid)
        {
            Type t = typeof(XML2);
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return await GenerateSqlAddMultipleObj(xmlData, props, table, csytid);
        }

        async Task<ResultInfo> ThemDvkt(IEnumerable<XElement> xmlData, string table, int csytid)
        {
            Type t = typeof(XML3);
            PropertyInfo[] props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return await GenerateSqlAddMultipleObj(xmlData, props, table, csytid);
        }

        private async Task<ResultInfo> GenerateSqlAddSingleObj(XElement xmlData, PropertyInfo[] props, string table, int csytid)
        {
            var sql = $"INSERT INTO {table} ";
            var cols = "(";
            var vals = "(";
            string[] exceptProps =new string[] { "XML1ID", "XML2ID", "XML3ID" };
            for(int i=0; i < props.Count(); i++)
            {
                var p = props[i];
                var pName = p.Name.ToUpper();
                if (!exceptProps.Contains(pName)) {
                    cols += pName;
                    vals += $"@{pName}";
                    if (i != props.Count() - 1)
                    {
                        cols += ",";
                        vals += ",";
                    }
                    else
                    {
                        cols += ")";
                        vals += ")";
                    }
                }
            }
            
            sql = $"{sql} {cols} VALUE {vals}"; // raw sql with params
            var conn = _dbContext.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            foreach ( var p in props )
            {
                Type propertyType = p.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType);
                var pTypeName = underlyingType != null ? underlyingType.Name : propertyType.Name;
                var pName = p.Name.ToUpper();
                if (!exceptProps.Contains(p.Name))
                {
                    if (pName == "CSYTID")
                    {
                        cmd.Parameters.Add(new MySqlParameter(pName, csytid));
                    }
                    else
                    {
                        switch (pTypeName)
                        {
                            case "Int32": /// kiểu dữ liệu int
                                cmd.Parameters.Add(new MySqlParameter(pName, GetInt(xmlData.Element(pName))));
                                break;
                            case "String": /// kiểu dữ liệu String
                                cmd.Parameters.Add(new MySqlParameter(pName, $"{ReplaceCData((string?)xmlData.Element(pName) ?? "")}"));
                                break;
                            case "DateTime": /// kiểu dữ liệu DateTime
                                var temp = GetLong(xmlData.Element(pName)) != 0 ? ConvertCompactTimestampToDateTime(GetLong(xmlData.Element(pName))) : ConvertCompactTimestampToDateTime(180001010000);
                                cmd.Parameters.Add(new MySqlParameter(pName, $"{temp.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)}"));
                                break;
                            default:
                                cmd.Parameters.Add(new MySqlParameter(pName, ""));
                                break;

                        }
                    }
                }
            }
            ResultInfo result;
            DbTransaction transaction = conn.BeginTransaction();
            try
            {
                // Start a local transaction.
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
                transaction.Commit();
                return new ResultInfo { message = "Ok", status_code = 200 };
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
                return new ResultInfo { message = ex.Message, status_code = 500 };
            }  
        }
        private class MyProp
        {
            public string propName { get; set; }
            public string propDataType { get; set; }
        }
        
        private class ResultInfo
        {
            public int status_code { get; set; }
            public string message { get; set; }
        }
        private async Task<ResultInfo> GenerateSqlAddMultipleObj(IEnumerable<XElement> xmlDataArr, PropertyInfo[] props, string table, int csytid)
        {
            var sql = $"INSERT INTO {table} ";
            var cols = "(";
            var vals = "";
            var first = 1;
            string[] exceptProps = new string[] { "XML1ID", "XML2ID", "XML3ID" };
            // chuyển thông tin thuộc tính trong class thành dạng dictionary
            List<MyProp> mappedProps = new List<MyProp>();
            foreach(var p in props)
            {
                Type propertyType = p.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType);
                var pTypeName = underlyingType != null ? underlyingType.Name : propertyType.Name;
                mappedProps.Add(new MyProp { propName = p.Name.ToUpper(), propDataType = pTypeName });
            }
            var j = 0;
            foreach(var xmlData in xmlDataArr)
            {
                vals += "(";
                for (int i = 0; i < mappedProps.Count; i++)
                {
                    var p = mappedProps[i];
                    
                    if (!exceptProps.Contains(p.propName))
                    {
                        if(first == 1) cols += p.propName;
                        vals += $"@{p.propName}{j}";
                        if (i != props.Count() - 1)
                        {
                            if( first == 1)cols += ",";
                            vals += ",";
                        }
                        else
                        {
                            if (first == 1) cols += ")";
                            vals += ")";
                        }
                    }
                }
                j++;
                first = 0;
                vals += ",";
            }
            vals = vals.Remove(vals.Length - 1);
            sql = $"{sql} {cols} VALUES {vals}";
            var conn = _dbContext.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            j = 0;
            foreach (var xmlData in xmlDataArr)
            {
                for (int i = 0; i < mappedProps.Count; i++)
                {
                    var p = mappedProps[i];

                    if (!exceptProps.Contains(p.propName))
                    {
                        if (p.propName == "CSYTID")
                        {
                            cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", csytid));
                        }
                        else
                        {
                            switch (p.propDataType)
                            {
                                case "Int32": /// kiểu dữ liệu int
                                    cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", GetInt(xmlData.Element(p.propName))));
                                    break;
                                case "String": /// kiểu dữ liệu String
                                    cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", $"{ReplaceCData((string?)xmlData.Element(p.propName) ?? "")}"));
                                    break;
                                case "DateTime": /// kiểu dữ liệu DateTime
                                    var temp = GetLong(xmlData.Element(p.propName)) != 0 ? ConvertCompactTimestampToDateTime(GetLong(xmlData.Element(p.propName))) : ConvertCompactTimestampToDateTime(180001010000);
                                    cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", $"{temp.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)}"));
                                    break;
                                default:
                                    cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", ""));
                                    break;
                            }
                        }
                    }
                }
                j++;
            }
            ResultInfo result;
            DbTransaction transaction = conn.BeginTransaction();
            try
            {
                cmd.Transaction = transaction;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                transaction.Commit();
                return new ResultInfo { message = "Ok", status_code = 200 };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
                return new ResultInfo { message = ex.Message, status_code = 500 };
            }
        }
        
    }
}
