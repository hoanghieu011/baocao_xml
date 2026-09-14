using api.Models;
using API.Common;
using API.Data;
using API.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using OfficeOpenXml;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Utilities;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        [Authorize(Roles = "ADMIN")]
        [HttpPost("GetImportExcelTablesInfo")]
        public async Task<IActionResult> GetImportExcelTablesInfo()
        {
            var tables = Enum.GetNames<EXCEL_TABLE>();
            var res = tables.Select(t =>
            {
                return new
                {
                    code = t,
                    name = GetTable(t)
                };
            });
            return Ok(res);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("GetImportExcelTemplate")]
        public async Task<IActionResult> GetImportExcelTemplate(string excelTable)
        {
            try
            {
                var tableStrs = Enum.GetNames<EXCEL_TABLE>();
                if (excelTable == null || (excelTable != null && !tableStrs.Contains(excelTable)))
                {
                    return BadRequest("Không xác định được bảng dữ liệu cần import!");
                }
                var headers = GetExcelTableHeader(excelTable);
                using var workbook = new XLWorkbook();
                var ws = workbook.AddWorksheet($"Mẫu {excelTable}");

                // Write headers
                for (int c = 0; c < headers.Count; c++)
                {
                    var cell1 = ws.Cell(1, c + 1);
                    cell1.Value = Enum.GetName(headers[c].type);
                    cell1.Style.Font.Bold = true;

                    var cell2 = ws.Cell(2, c + 1);
                    cell2.Value = headers[c].name;
                    cell2.Style.Font.Bold = true;
                }
                //var tableRange = ws.Range(1, 1, 1, headers.Count);

                //var table = ws.Range(1, 1, 1, headers.Count)
                //               .CreateTable();

                ws.Columns().AdjustToContents();

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"MAU_{excelTable}.xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
        }
        public enum TEMPLATE_EXCEL_HEADER_DATA_TYPE {
            INT32,
            DECIMAL,
            STRING,
            DATETIME
        }

        public class TEMPLATE_EXCEL_HEADER_PROPS
        {
            public string name { get; set; }
            public TEMPLATE_EXCEL_HEADER_DATA_TYPE type { get; set; }
        }

        private List<TEMPLATE_EXCEL_HEADER_PROPS> GetExcelTableHeader(string excelTable)
        {
            List<TEMPLATE_EXCEL_HEADER_PROPS> headers = [];
            var tableStrs = Enum.GetNames<EXCEL_TABLE>();
            if (Enum.GetName(EXCEL_TABLE.BNND) == excelTable)
            {
                headers = [
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "MA_LK", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "MAHOSOBENHAN", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "LOAIPHIEUMAUBENHPHAM", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "MADICHVU", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "TENDICHVU", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "KHOAID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "PHONGID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NGUOIDUNGID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NGUOITRAKETQUA", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "MA_BAC_SI", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NGUOI_THUC_HIEN", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "DICHVUID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NHOM_MABHYT_ID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "TEN_DVT", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "SOLUONG", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.DECIMAL },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "TIEN_NHANDAN", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.DECIMAL },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NGAY_RAVIEN", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.DATETIME },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "NGAY_TIEPNHAN", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.DATETIME }
                    ];
            }
            else if(Enum.GetName(EXCEL_TABLE.BN15T) == excelTable)
            {
                headers = [
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "BACSIID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "THANGNAM", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "BHYT", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "SOLUONG", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 }
                    ];
            }
            else if (Enum.GetName(EXCEL_TABLE.BN_NHAPVIEN) == excelTable)
            {
                headers = [
                       new TEMPLATE_EXCEL_HEADER_PROPS { name = "BACSIID", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "THANGNAM", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "BHYT", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 },
                        new TEMPLATE_EXCEL_HEADER_PROPS { name = "SOLUONG", type = TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 }
                   ];
            }
            return headers;
        }
        
        public class ImportExcelHospitalDataRequest
        {
            public IFormFile file {get; set;}
            public string excelTable {get; set;}
        }

        [Authorize(Roles = "ADMIN")]
        [RequestSizeLimit(1024 * 1024)]
        [HttpPost("ImportExcelHospitalData")]
        public async Task<IActionResult> ImportExcelHospitalData([FromForm] ImportExcelHospitalDataRequest req)
        {
            var file = req.file;
            var excelTable = req.excelTable;
            await Task.Delay(2000);
            //return BadRequest(new ImportExcelResponse
            //    {
            //        message= "test fake error!",
            //        isError = true,
            //    });
           
            var tableStrs = Enum.GetNames<EXCEL_TABLE>();
            if (excelTable == null || (excelTable != null && !tableStrs.Contains(excelTable)))
            {
                return BadRequest(new ImportExcelResponse
                {
                    message= "Không xác định được bảng dữ liệu cần import!",
                    isError = true,
                });
            }
            if (file == null || file.Length <= 0)
            {
                return BadRequest(new ImportExcelResponse
                {
                    message= "File rỗng hoặc không hợp lệ.",
                    isError = true,
                });
            }
            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new ImportExcelResponse
                {
                    message= "Vui lòng Upload file .xlsx hoặc file .xls!",
                    isError = true,
                });
            }
            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

            if (string.IsNullOrEmpty(userName))
                return Unauthorized();

            // Lấy tên database động thông qua service dùng chung
            var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
			if (string.IsNullOrEmpty(dbData))
                return BadRequest(new ImportExcelResponse
                    {
                        message= "Không xác định được database dữ liệu cho user.",
                        isError = true,
                    });
            // Lấy csyt Id động thông qua service dùng chung
            var tempCsytId = await _dbResolver.GetCsytIdByUserAsync(userName);
            if (string.IsNullOrEmpty(tempCsytId))
                return BadRequest(new ImportExcelResponse
                        {
                            message= "Không xác định được csyt cho user.",
                            isError = true,
                        });
            var csytId = 0;
            if (int.TryParse(tempCsytId, out int value))
            {
                csytId = value;
            }
            // Validate identifier (chỉ cho phép chữ, số, underscore)
            if (!Regex.IsMatch(dbData, @"^[A-Za-z0-9_]+$"))
            return BadRequest(new ImportExcelResponse
                        {
                            message= "Tên database không hợp lệ.",
                            isError = true,
                        });

            var table = GetTable(excelTable);
             //return Ok(new ImportExcelResponse
             //   {
             //       message= "test ok",
             //       affectedRows = 10,
             //       table = table,
             //   });
            if(table == "") return BadRequest(new ImportExcelResponse
                        {
                            message= "Không xác định được bảng dữ liệu cần import!",
                            isError = true,
                        });

            var dropTblTemp = $"DROP TABLE IF EXISTS `{dbData}`.like_{table}";
            // drop temp table
            await _dbContext.Database.ExecuteSqlRawAsync(dropTblTemp);
            // tạo bảng tạm và copy dữ liệu từ bảng gốc sang
            var createTblTempSql = $"CREATE TABLE `{dbData}`.like_{table} LIKE `{dbData}`.{table}";
            var insertTblTempSql = $"INSERT INTO `{dbData}`.like_{table} SELECT * FROM `{dbData}`.{table}";

            await _dbContext.Database.ExecuteSqlRawAsync(createTblTempSql);
            await _dbContext.Database.ExecuteSqlRawAsync(insertTblTempSql);

            var res = await InsertExcelData(excelTable, file, dbData);
            
            res.table = table;
           
            ActionResult httpRes = Ok(res);
            if (res.isError)
            {
                // revert dữ liệu
                // xóa dữ liệu bảng gốc ( dữ liệu trước import + dữ liệu trong import(nếu có))
                var sqlTruncate = $"TRUNCATE TABLE `{dbData}`.{table}";
                // insert lại dữ liệu trước import
                var revertInsert = $"INSERT INTO `{dbData}`.{table} SELECT * FROM `{dbData}`.like_{table}";
                
                await _dbContext.Database.ExecuteSqlRawAsync(sqlTruncate);
                await _dbContext.Database.ExecuteSqlRawAsync(revertInsert);
                httpRes = BadRequest(
                    res
                );
            }
            // drop temp table
            await _dbContext.Database.ExecuteSqlRawAsync(dropTblTemp);
            return httpRes;
        }

        public enum EXCEL_TABLE
        {
            BNND,
            BN15T,
            BN_NHAPVIEN
        }
        private string GetTable(string excelTable)
        {
            var tableStrs = Enum.GetNames<EXCEL_TABLE>();
            if (excelTable == null) return "";
            else if (excelTable == Enum.GetName(EXCEL_TABLE.BNND)) return "xml_bnnd";
            else if (excelTable == Enum.GetName(EXCEL_TABLE.BN15T)) return "bc_benhnhan_15t";
            else if (excelTable == Enum.GetName(EXCEL_TABLE.BN_NHAPVIEN)) return "bc_benhnhan_nhapvien";
            else return "";
        }

        private async Task<ImportExcelResponse> InsertExcelData(string excelTable, IFormFile file, string dbName)
        {
            return await InsertRows(excelTable, file, dbName);
        }

        private async Task<ImportExcelResponse> InsertRows(string excelTable,IFormFile file, string dbName)
        {
            try
            {
                var affectedRows = 0;
                var table = GetTable(excelTable);
                var headers = GetExcelTableHeader(excelTable);
                if (table=="")
                {
                    return new ImportExcelResponse
                    {
                        isError = true,
                        message = "Không xác định được bảng"
                    };
                }

                using (var stream = file.OpenReadStream())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var headerInfo = FindExcelHeader(worksheet, headers);
                        if (headerInfo == null)
                        {
                            return new ImportExcelResponse
                            {
                                isError = true,
                                message = $"Không tìm thấy dòng tiêu đề hợp lệ. Các cột bắt buộc: {string.Join(", ", headers.Select(h => h.name))}."
                            };
                        }

                        var insertSQL = $"INSERT INTO `{dbName}`.`{table}` (" +
                            string.Join(", ", headers.Select(header => $"`{header.name}`")) +
                            ") VALUES ";
                        var valueRows = new List<string>();
                        var parameters = new List<MySqlParameter>();

                        foreach (var row in worksheet.RowsUsed()
                            .Where(row => row.RowNumber() > headerInfo.RowNumber))
                        {
                            var mappedCells = headers
                                .Select((header, index) => new
                                {
                                    Header = header,
                                    Cell = row.Cell(headerInfo.ColumnByHeaderIndex[index])
                                })
                                .ToList();

                            if (mappedCells.All(item => item.Cell.IsEmpty()))
                            {
                                continue;
                            }

                            var rowParameters = new List<string>();
                            foreach (var item in mappedCells)
                            {
                                var paramName = $"@{item.Header.name}_{affectedRows}";
                                rowParameters.Add(paramName);
                                try
                                {
                                    parameters.Add(CreateExcelParameter(paramName, item.Cell, item.Header.type));
                                }
                                catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                                {
                                    throw new FormatException(
                                        $"Dữ liệu không hợp lệ tại dòng {row.RowNumber()}, cột {item.Header.name}: '{item.Cell.GetString()}'.",
                                        ex
                                    );
                                }
                            }

                            valueRows.Add($"({string.Join(", ", rowParameters)})");
                            affectedRows++;
                        }

                        if (affectedRows == 0)
                        {
                            return new ImportExcelResponse
                            {
                                isError = true,
                                message = "Không có dữ liệu bên dưới dòng tiêu đề để import."
                            };
                        }

                        await _dbContext.Database.ExecuteSqlRawAsync(
                            insertSQL + string.Join(",", valueRows) + ";",
                            parameters.ToArray()
                        );
                    }
                }
                return new ImportExcelResponse
                {
                    isError = false,
                    message = "Thành công!",
                    affectedRows = affectedRows

                };
            }
            catch (Exception ex)
            {
                return new ImportExcelResponse
                {
                    isError = true,
                    message = ex.Message
                };
            }
        }

        private sealed class ExcelHeaderInfo
        {
            public int RowNumber { get; init; }
            public Dictionary<int, int> ColumnByHeaderIndex { get; init; } = [];
        }

        private ExcelHeaderInfo? FindExcelHeader(
            IXLWorksheet worksheet,
            IReadOnlyList<TEMPLATE_EXCEL_HEADER_PROPS> headers)
        {
            foreach (var row in worksheet.RowsUsed().Take(50))
            {
                var columnByNormalizedName = row.CellsUsed()
                    .Select(cell => new
                    {
                        Name = NormalizeExcelHeader(cell.GetString()),
                        ColumnNumber = cell.Address.ColumnNumber
                    })
                    .Where(item => item.Name != "")
                    .GroupBy(item => item.Name)
                    .ToDictionary(group => group.Key, group => group.First().ColumnNumber);

                var columnByHeaderIndex = new Dictionary<int, int>();
                for (var headerIndex = 0; headerIndex < headers.Count; headerIndex++)
                {
                    var matchedColumn = GetExcelHeaderAliases(headers[headerIndex].name)
                        .Select(alias => NormalizeExcelHeader(alias))
                        .Where(columnByNormalizedName.ContainsKey)
                        .Select(alias => columnByNormalizedName[alias])
                        .FirstOrDefault();

                    if (matchedColumn == 0)
                    {
                        columnByHeaderIndex.Clear();
                        break;
                    }

                    columnByHeaderIndex[headerIndex] = matchedColumn;
                }

                if (columnByHeaderIndex.Count == headers.Count)
                {
                    return new ExcelHeaderInfo
                    {
                        RowNumber = row.RowNumber(),
                        ColumnByHeaderIndex = columnByHeaderIndex
                    };
                }
            }

            return null;
        }

        private static IEnumerable<string> GetExcelHeaderAliases(string headerName)
        {
            return headerName switch
            {
                "MA_LK" => ["MA_LK", "Tiếp nhận Id", "Mã liên kết"],
                "MAHOSOBENHAN" => ["MAHOSOBENHAN", "Mã hồ sơ bệnh án"],
                "LOAIPHIEUMAUBENHPHAM" => ["LOAIPHIEUMAUBENHPHAM", "Loại phiếu mẫu bệnh phẩm"],
                "MADICHVU" => ["MADICHVU", "Mã dịch vụ"],
                "TENDICHVU" => ["TENDICHVU", "Tên dịch vụ"],
                "KHOAID" => ["KHOAID", "Khoa Id"],
                "PHONGID" => ["PHONGID", "Phòng Id"],
                "NGUOIDUNGID" => ["NGUOIDUNGID", "Người dùng Id"],
                "NGUOITRAKETQUA" => ["NGUOITRAKETQUA", "Người trả KQ", "Người trả kết quả"],
                "MA_BAC_SI" => ["MA_BAC_SI", "Mã Bác sĩ"],
                "NGUOI_THUC_HIEN" => ["NGUOI_THUC_HIEN", "Người thực hiện"],
                "DICHVUID" => ["DICHVUID", "Dịch vụ Id"],
                "NHOM_MABHYT_ID" => ["NHOM_MABHYT_ID", "Nhóm mã BHYT Id"],
                "TEN_DVT" => ["TEN_DVT", "Tên Đvt", "Tên đơn vị tính"],
                "BACSIID" => ["BACSIID", "Bác sĩ", "Mã bác sĩ"],
                "THANGNAM" => ["THANGNAM", "Tháng năm"],
                "BHYT" => ["BHYT", "Bảo hiểm y tế"],
                "SOLUONG" => ["SOLUONG", "Số lượng"],
                "TIEN_NHANDAN" => ["TIEN_NHANDAN", "Tiền nhân dân"],
                "NGAY_RAVIEN" => ["NGAY_RAVIEN", "Ngày ra viện"],
                "NGAY_TIEPNHAN" => ["NGAY_TIEPNHAN", "Ngày tiếp nhận"],
                _ => [headerName]
            };
        }

        private static string NormalizeExcelHeader(string value)
        {
            var normalized = value.Trim().ToUpperInvariant()
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark &&
                    char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static MySqlParameter CreateExcelParameter(
            string paramName,
            IXLCell cell,
            TEMPLATE_EXCEL_HEADER_DATA_TYPE dataType)
        {
            var cellValue = cell.GetString().Trim();

            return dataType switch
            {
                TEMPLATE_EXCEL_HEADER_DATA_TYPE.INT32 => new MySqlParameter(paramName, MySqlDbType.Int32)
                {
                    Value = cell.IsEmpty() ? 0 : Convert.ToInt32(cellValue, CultureInfo.InvariantCulture)
                },
                TEMPLATE_EXCEL_HEADER_DATA_TYPE.DECIMAL => new MySqlParameter(paramName, MySqlDbType.Decimal)
                {
                    Value = cell.IsEmpty() ? 0m : Convert.ToDecimal(cellValue, CultureInfo.InvariantCulture)
                },
                TEMPLATE_EXCEL_HEADER_DATA_TYPE.STRING => new MySqlParameter(paramName, MySqlDbType.VarChar)
                {
                    Value = cellValue
                },
                TEMPLATE_EXCEL_HEADER_DATA_TYPE.DATETIME => new MySqlParameter(paramName, MySqlDbType.DateTime)
                {
                    Value = GetExcelDateTimeValue(cell, cellValue)
                },
                _ => throw new InvalidOperationException($"Kiểu dữ liệu Excel không được hỗ trợ: {dataType}.")
            };
        }

        private static object GetExcelDateTimeValue(IXLCell cell, string cellValue)
        {
            if (cell.IsEmpty())
            {
                return DBNull.Value;
            }

            if (cell.TryGetValue<DateTime>(out var dateTime))
            {
                return dateTime;
            }

            string[] acceptedFormats = ["dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"];
            if (DateTime.TryParseExact(
                cellValue,
                acceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dateTime))
            {
                return dateTime;
            }

            throw new FormatException($"Giá trị ngày không hợp lệ: '{cellValue}'.");
        }
        private ImportExcelResponse InsertBnNhapVien(string excelTable, IFormFile file, string dbName)
        {
            try
            {
                var affectedRows = 0;
                return new ImportExcelResponse
                {
                    isError = false,
                    message = "Thành công!",
                    affectedRows = affectedRows

                };
            }
            catch (Exception ex)
            {
                return new ImportExcelResponse
                {
                    isError = true,
                    message = ex.Message
                };
            }
        }

        [Authorize(Roles ="ADMIN")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [HttpPost("ImportXMLHospitalData")]
        public async Task<IActionResult> ImportXMLHospitalData([FromForm] IFormFile file)
        {
            await Task.Delay(5000);
            //return Ok(new
            //     ImportXMLResponse
            //    {
            //        message = "test xml import ok!",
            //        countXML1 = 1,
            //        countXML2 = 1,
            //        countXML3 = 1,
            //        isError = false
            //    });
            if (file == null || file.Length <= 0)
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = "File rỗng hoặc không hợp lệ."
                });
            }
            if (!file.FileName.EndsWith(".xml"))
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = "Vui lòng Upload file .xml!"
                });
            }
            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

            if (string.IsNullOrEmpty(userName))
                return Unauthorized(new ImportXMLResponse
                {
                    isError = true,
                    message="Không xác minh được người dùng!"
                });

            // Lấy tên database động thông qua service dùng chung
            var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);
			if (string.IsNullOrEmpty(dbData)) 
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = "Không xác định được database dữ liệu cho user."
                });

            // Lấy csyt Id động thông qua service dùng chung
            var tempCsytId = await _dbResolver.GetCsytIdByUserAsync(userName);
            if (string.IsNullOrEmpty(tempCsytId))
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = "Không xác định được csyt cho user."
                });
            var csytId = 0;
            if (int.TryParse(tempCsytId, out int value))
            {
                csytId = value;
            }
            // Validate identifier (chỉ cho phép chữ, số, underscore)
            if (!Regex.IsMatch(dbData, @"^[A-Za-z0-9_]+$")) 
                return BadRequest(new ImportXMLResponse
                    {
                        isError = true,
                        message = "Tên database không hợp lệ."
                    });
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, Async = true };
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = XmlReader.Create(stream, settings);
                var msg = "";
                var countXml1 = 0;
                var countXml2 = 0;
                var countXml3 = 0;
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
                            countXml1++;
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
                            var dsChiTietThuocXml = chiTietThuocXmWrapper.Elements("CHI_TIET_THUOC").ToList();
                            countXml2 += dsChiTietThuocXml.Count;
                            resThemDsThuoc = await ThemChiTietThuoc(dsChiTietThuocXml, $"`{dbData}`.xml2", csytId);
                        }
                        else if (loai.Equals("XML3"))
                        {
                            var chiTietDvktXmWrapper = noidung.Element("DSACH_CHI_TIET_DVKT");
                            var dsChiTietDvktXml = chiTietDvktXmWrapper.Elements("CHI_TIET_DVKT").ToList();
                            countXml3 += dsChiTietDvktXml.Count;
							resThemDsDvkt = await ThemDvkt(dsChiTietDvktXml, $"`{dbData}`.xml3", csytId);
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
                            var sqlDel1 = $"DELETE FROM `{dbData}`.xml1 WHERE MA_LK={maLK}";
                            var sqlDel2 = $"DELETE FROM `{dbData}`.xml2 WHERE MA_LK={maLK}";
                            var sqlDel3 = $"DELETE FROM `{dbData}`.xml3 WHERE MA_LK={maLK}";
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel1);
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel2);
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel3);
                        }
                        return StatusCode(500, new ImportXMLResponse
                        {
                            message = $"Lỗi SQL: ở {maLK} : {msg}",
                            isError = true
                        });
                    }
                }
                msg = "Thêm mới thành công!";
                return Ok(new
                 ImportXMLResponse
                {
                    message = msg,
                    countXML1 = countXml1,
                    countXML2 = countXml2,
                    countXML3 = countXml3,
                    isError = false
                });
            }
            catch (XmlException xe)
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = $"Lỗi XML: " + xe.Message
                });
            }
            catch (FormatException fe)
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = $"Lỗi Base64: " + fe.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return StatusCode(500, new ImportXMLResponse
                {
                    isError = true,
                    message = "Lỗi server: " + ex.Message
                });
            }

        }

        private async Task<IActionResult> ImportXMLHospitalData_old(IFormFile file)
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
                        var countXml1 = 0;
                        var countXml2 = 0;
                        var countXml3 = 0;
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
                            countXml1++;
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
                                    countXml2++;
                            var chiTietThuocXmWrapper = noidung.Element("DSACH_CHI_TIET_THUOC");
                            var dsChiTietThuocXml = chiTietThuocXmWrapper.Elements("CHI_TIET_THUOC");
                            resThemDsThuoc = await ThemChiTietThuoc(dsChiTietThuocXml, $"`{dbData}`.xml2", csytId);
                        }
                        else if (loai.Equals("XML3"))
                        {
                                    countXml3++;
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
                            var sqlDel1 = $"DELETE FROM `{dbData}`.xml1 WHERE MA_LK={maLK}";
                            var sqlDel2 = $"DELETE FROM `{dbData}`.xml2 WHERE MA_LK={maLK}";
                            var sqlDel3 = $"DELETE FROM `{dbData}`.xml3 WHERE MA_LK={maLK}";
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel1);
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel2);
                            await _dbContext.Database.ExecuteSqlRawAsync(sqlDel3);
                        }
                        return StatusCode(500, $"Lỗi SQL: ở {maLK} : {msg}");
                    }
                }
                msg = "Thêm mới thành công!";
                return Ok( new
                 ImportXMLResponse{
                    message = msg,
                    countXML1 = countXml1,
                    countXML2 = countXml2,
                    countXML3 = countXml3,
                    isError = false
                });
            }
            catch (XmlException xe)
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = $"Lỗi XML: " + xe.Message
                });
            }
            catch (FormatException fe)
            {
                return BadRequest(new ImportXMLResponse
                {
                    isError = true,
                    message = $"Lỗi Base64: " + fe.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return StatusCode(500, new ImportXMLResponse
                {
                    isError = true,
                    message = "Lỗi server: " + ex.Message
                });
            }
            
        }

        /// <summary>
        /// Xóa các bản ghi xml1, xml2, xml3 theo tháng.
        /// Mốc thời gian lấy từ cột NGAY_RA của xml1; xml2, xml3 xóa theo MA_LK của các xml1 tương ứng.
        /// </summary>
        /// <param name="thang">Tháng cần xóa (1-12).</param>
        /// <param name="nam">Năm cần xóa.</param>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("DeleteHospitalDataByMonth")]
        public async Task<IActionResult> DeleteHospitalDataByMonth([FromQuery] int thang, [FromQuery] int nam)
        {
            if (thang < 1 || thang > 12)
                return BadRequest("Tháng không hợp lệ (1-12).");
            if (nam < 1900 || nam > 9999)
                return BadRequest("Năm không hợp lệ.");

            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("USER_NAME")?.Value;

            if (string.IsNullOrEmpty(userName))
                return Unauthorized();

            // Lấy tên database động thông qua service dùng chung
            var dbData = await _dbResolver.GetDatabaseByUserAsync(userName);

            if (string.IsNullOrEmpty(dbData))
                return BadRequest("Không xác định được database dữ liệu cho user.");

            // Validate identifier (chỉ cho phép chữ, số, underscore)
            if (!Regex.IsMatch(dbData, @"^[A-Za-z0-9_]+$"))
                return BadRequest("Tên database không hợp lệ.");

            // Điều kiện lọc xml1 theo tháng dựa vào NGAY_RA
            var whereXml1 = "YEAR(NGAY_RA) = @nam AND MONTH(NGAY_RA) = @thang";

            // Xóa con (xml2, xml3) theo MA_LK của các xml1 nằm trong tháng, sau đó mới xóa cha (xml1)
            var sqlDelXml2 = $"DELETE FROM `{dbData}`.xml2 WHERE MA_LK IN (SELECT MA_LK FROM `{dbData}`.xml1 WHERE {whereXml1})";
            var sqlDelXml3 = $"DELETE FROM `{dbData}`.xml3 WHERE MA_LK IN (SELECT MA_LK FROM `{dbData}`.xml1 WHERE {whereXml1})";
            var sqlDelXml1 = $"DELETE FROM `{dbData}`.xml1 WHERE {whereXml1}";

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var soXml2 = await _dbContext.Database.ExecuteSqlRawAsync(sqlDelXml2,
                    new MySqlParameter("@nam", nam), new MySqlParameter("@thang", thang));
                var soXml3 = await _dbContext.Database.ExecuteSqlRawAsync(sqlDelXml3,
                    new MySqlParameter("@nam", nam), new MySqlParameter("@thang", thang));
                var soXml1 = await _dbContext.Database.ExecuteSqlRawAsync(sqlDelXml1,
                    new MySqlParameter("@nam", nam), new MySqlParameter("@thang", thang));

                await transaction.CommitAsync();
                return Ok($"Đã xóa dữ liệu tháng {thang}/{nam}: {soXml1} bản ghi xml1, {soXml2} bản ghi xml2, {soXml3} bản ghi xml3.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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

        static decimal GetDecimal(XElement? e)
        {
            if (e == null) return 0;
            if (decimal.TryParse(e.Value.Trim(), CultureInfo.InvariantCulture, out var v)) return v;
            return 0;
            throw new Exception("GetDecimal exception: " + e);
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
                if (!exceptProps.Contains(pName))
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
                            case "Decimal": /// kiểu dữ liệu decimal
                                cmd.Parameters.Add(new MySqlParameter(pName, GetDecimal(xmlData.Element(pName))));
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
            //return new ResultInfo { message = "Ok", status_code = 200 };
            DbTransaction transaction = conn.BeginTransaction();
            try
            {
                // Start a local transaction.
                cmd.Transaction = transaction;
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
                                case "Decimal": /// kiểu dữ liệu decimal
                                    cmd.Parameters.Add(new MySqlParameter($"{p.propName}{j}", GetDecimal(xmlData.Element(p.propName))));
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
            //return new ResultInfo { message = "Ok", status_code = 200 };
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
