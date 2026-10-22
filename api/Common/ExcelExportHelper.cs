using ClosedXML.Excel;

namespace API.Common
{
    /// <summary>
    /// Định nghĩa một cột khi xuất Excel cho màn hình danh sách phẳng.
    /// </summary>
    /// <typeparam name="T">Kiểu của một dòng dữ liệu.</typeparam>
    public class ExcelColumn<T>
    {
        /// <summary>Tiêu đề cột hiển thị ở dòng header.</summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>Hàm lấy giá trị của ô từ một dòng dữ liệu.</summary>
        public Func<T, object?> Value { get; set; } = _ => null;

        /// <summary>
        /// Buộc ghi dưới dạng text (giữ số 0 ở đầu cho các mã định danh).
        /// </summary>
        public bool AsText { get; set; }

        /// <summary>Định dạng số/ngày của ClosedXML (vd: "#,##0", "#,##0.00", "dd/MM/yyyy").</summary>
        public string? NumberFormat { get; set; }

        /// <summary>Độ rộng cột; null = tự co theo nội dung.</summary>
        public double? Width { get; set; }
    }

    /// <summary>
    /// Helper dùng chung để sinh file Excel (.xlsx) cho các màn hình danh sách.
    /// Chịu trách nhiệm: header, định dạng ô, Unicode tiếng Việt, giữ số 0 ở đầu,
    /// xử lý ngày/số/bool/giá trị rỗng và trường hợp dữ liệu rỗng.
    /// Phần đặc thù từng màn hình (nguồn dữ liệu, danh sách cột, tên file) do caller cung cấp.
    /// </summary>
    public static class ExcelExportHelper
    {
        /// <summary>MIME type chuẩn cho file .xlsx — dùng khi trả về File(...).</summary>
        public const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        /// <summary>
        /// Sinh workbook từ danh sách dữ liệu và cấu hình cột.
        /// </summary>
        public static byte[] Build<T>(
            IEnumerable<T>? rows,
            IReadOnlyList<ExcelColumn<T>> columns,
            string sheetName = "DATA",
            bool includeIndexColumn = true,
            string indexHeader = "STT")
        {
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("Phải cung cấp ít nhất một cột.", nameof(columns));

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(SafeSheetName(sheetName));

            var dataList = rows?.ToList() ?? new List<T>();

            if (dataList.Count == 0)
            {
                ws.Cell(1, 1).Value = "Không có dữ liệu";
                return Save(wb);
            }

            // ----- Header -----
            var col = 1;
            if (includeIndexColumn)
            {
                ws.Cell(1, col).Value = indexHeader;
                col++;
            }
            foreach (var c in columns)
            {
                ws.Cell(1, col).Value = c.Header;
                col++;
            }

            var headerRange = ws.Range(1, 1, 1, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // ----- Dữ liệu -----
            var rowIdx = 2;
            foreach (var item in dataList)
            {
                col = 1;
                if (includeIndexColumn)
                {
                    ws.Cell(rowIdx, col).Value = rowIdx - 1;
                    col++;
                }

                foreach (var c in columns)
                {
                    var cell = ws.Cell(rowIdx, col);
                    SetCellValue(cell, c.Value(item), c);
                    col++;
                }
                rowIdx++;
            }

            var dataRange = ws.Range(1, 1, rowIdx - 1, columns.Count + (includeIndexColumn ? 1 : 0));
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // ----- Độ rộng cột -----
            col = 1;
            if (includeIndexColumn)
            {
                ws.Column(col).Width = 6;
                col++;
            }
            foreach (var c in columns)
            {
                if (c.Width.HasValue) ws.Column(col).Width = c.Width.Value;
                else ws.Column(col).AdjustToContents();
                col++;
            }

            ws.SheetView.FreezeRows(1);
            return Save(wb);
        }

        private static void SetCellValue<T>(IXLCell cell, object? value, ExcelColumn<T> col)
        {
            if (value == null)
            {
                cell.Value = string.Empty;
                return;
            }

            // Giữ số 0 ở đầu cho mã định danh: ghi text, chặn Excel diễn giải thành số.
            if (col.AsText)
            {
                cell.SetValue(value.ToString() ?? string.Empty);
                cell.Style.NumberFormat.Format = "@";
                return;
            }

            switch (value)
            {
                case DateTime dt:
                    cell.Value = dt;
                    cell.Style.NumberFormat.Format = col.NumberFormat ?? "dd/MM/yyyy";
                    break;
                case bool b:
                    cell.Value = b ? "Có" : "Không";
                    break;
                case sbyte or byte or short or ushort or int or uint or long or ulong
                     or float or double or decimal:
                    cell.Value = Convert.ToDecimal(value);
                    if (!string.IsNullOrEmpty(col.NumberFormat))
                        cell.Style.NumberFormat.Format = col.NumberFormat;
                    break;
                default:
                    var s = value.ToString() ?? string.Empty;
                    // Chặn injection công thức khi mở bằng Excel (=, +, -, @ đầu chuỗi).
                    if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@'))
                    {
                        cell.SetValue("'" + s);
                    }
                    else
                    {
                        cell.Value = s;
                    }
                    break;
            }
        }

        private static byte[] Save(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        /// <summary>Tên sheet hợp lệ (tối đa 31 ký tự, bỏ ký tự cấm của Excel).</summary>
        private static string SafeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "DATA";
            foreach (var ch in new[] { '\\', '/', '*', '?', ':', '[', ']' })
                name = name.Replace(ch, '_');
            return name.Length > 31 ? name.Substring(0, 31) : name;
        }
    }
}
