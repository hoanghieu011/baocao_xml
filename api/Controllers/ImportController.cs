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
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Telegram.BotAPI.AvailableTypes;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        public ImportController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

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
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, Async = true };
                try
                {
                    using var stream = file.OpenReadStream();
                    using var reader = XmlReader.Create(stream, settings);
                var msg = "";
                    // đọc xml
                    while (reader.Read())
                    {
                        var csytId = 0;
                        if (reader.Name == "MACSKCB")
                        {
                   
                            var val = await reader.ReadElementContentAsStringAsync();
                            if (int.TryParse(val.Trim(), out var parsed)) csytId = parsed;
                            continue; 
                        }
                        if (reader.NodeType != XmlNodeType.Element || reader.Name != "HOSO")
                            continue;

                        var hosoEl = XElement.ReadFrom(reader) as XElement;
                        if (hosoEl == null) continue;

                        var bn = new XML1();
                        var dsChiTietThuoc = new List<XML2>();
                        var dsDichVuKiThuat = new List<XML3>();
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

                                    // Check existence by MA_LK (adjust to your key)
                                    var exists = await _dbContext.xml1
                                                    .AnyAsync(x => x.MA_LK == maLK);
                                    if (exists)
                                        continue;
                                    var benhNhan = new XML1
                                    {
                                        MA_LK = maLK,
                                        STT = GetInt(noidung.Element("STT")),
                                        MA_BN = (string?)noidung.Element("MA_BN") ?? "",
                                        HO_TEN = ReplaceCData(((string?)noidung.Element("HO_TEN") ?? "")),
                                        SO_CCCD = (string?)noidung.Element("SO_CCCD") ?? "",
                                        //NGAY_SINH = ConvertCompactTimestampToStr(GetLong(noidung.Element("NGAY_SINH")), "dd-MM-yyyy"),
                                        NGAY_SINH = (string?)(noidung.Element("NGAY_SINH")),
                                        GIOI_TINH = GetInt(noidung.Element("GIOI_TINH")),
                                        NHOM_MAU = (string?)noidung.Element("NHOM_MAU") ?? "",
                                        MA_QUOCTICH = (string?)noidung.Element("MA_QUOCTICH") ?? "",
                                        MA_DANTOC = (string?)noidung.Element("MA_DANTOC") ?? "",
                                        MA_NGHE_NGHIEP = (string?)noidung.Element("MA_NGHE_NGHIEP") ?? "",
                                        DIA_CHI = ReplaceCData( ((string?)noidung.Element("DIA_CHI") ?? "")),
                                        MATINH_CU_TRU = (string?)noidung.Element("MATINH_CU_TRU") ?? "",
                                        MAHUYEN_CU_TRU = (string?)noidung.Element("MAHUYEN_CU_TRU") ?? "",
                                        MAXA_CU_TRU = (string?)noidung.Element("MAXA_CU_TRU") ?? "",
                                        DIEN_THOAI = (string?)noidung.Element("DIEN_THOAI") ?? "",
                                        MA_THE_BHYT = (string?)noidung.Element("MA_THE_BHYT") ?? "",
                                        MA_DKBD = (string?)noidung.Element("MA_DKBD") ?? "",
                                        GT_THE_TU = (string?)noidung.Element("GT_THE_TU") ?? "",
                                        GT_THE_DEN = (string?)noidung.Element("GT_THE_DEN") ?? "",
                                        //NGAY_MIEN_CCT = ConvertCompactTimestampToStr(GetLong(noidung.Element("NGAY_MIEN_CCT")), "dd-MM-yyyy"),
                                        NGAY_MIEN_CCT = (string?)(noidung.Element("NGAY_MIEN_CCT")),
                                        LY_DO_VV = (string?)noidung.Element("LY_DO_VV") ?? "",
                                        LY_DO_VNT = (string?)noidung.Element("LY_DO_VNT") ?? "",
                                        MA_LY_DO_VNT = (string?)noidung.Element("MA_LY_DO_VNT") ?? "",
                                        CHAN_DOAN_VAO = ReplaceCData(((string?)noidung.Element("CHAN_DOAN_VAO") ?? "")),
                                        CHAN_DOAN_RV = ReplaceCData(((string?)noidung.Element("CHAN_DOAN_RV") ?? "")),
                                        MA_BENH_CHINH = (string?)noidung.Element("MA_BENH_CHINH") ?? "",
                                        MA_BENH_KT = (string?)noidung.Element("MA_BENH_KT") ?? "",
                                        MA_BENH_YHCT = (string?)noidung.Element("MA_BENH_YHCT") ?? "",
                                        MA_PTTT_QT = (string?)noidung.Element("MA_PTTT_QT") ?? "",
                                        MA_DOITUONG_KCB = (string?)noidung.Element("MA_DOITUONG_KCB") ?? "",
                                        MA_NOI_DI = (string?)noidung.Element("MA_NOI_DI") ?? "",
                                        MA_NOI_DEN = (string?)noidung.Element("MA_NOI_DEN") ?? "",
                                        MA_TAI_NAN = (string?)noidung.Element("MA_TAI_NAN") ?? "",
                                        NGAY_VAO = GetLong(noidung.Element("NGAY_VAO")) != 0 ? ConvertCompactTimestampToDateTime(GetLong(noidung.Element("NGAY_VAO"))) : ConvertCompactTimestampToDateTime(180001010000),
                                        NGAY_VAO_NOI_TRU = GetLong(noidung.Element("NGAY_VAO_NOI_TRU")) != 0 ? ConvertCompactTimestampToDateTime(GetLong(noidung.Element("NGAY_VAO_NOI_TRU"))) : ConvertCompactTimestampToDateTime(180001010000),
                                        NGAY_RA = GetLong(noidung.Element("NGAY_RA"))!=0 ? ConvertCompactTimestampToDateTime(GetLong(noidung.Element("NGAY_RA"))) : ConvertCompactTimestampToDateTime(180001010000),
                                        GIAY_CHUYEN_TUYEN = (string?)noidung.Element("GIAY_CHUYEN_TUYEN") ?? "",
                                        SO_NGAY_DTRI = (string?)noidung.Element("SO_NGAY_DTRI") ?? "",
                                        PP_DIEU_TRI = (string?)noidung.Element("PP_DIEU_TRI") ?? "",
                                        KET_QUA_DTRI = (string?)noidung.Element("KET_QUA_DTRI") ?? "",
                                        MA_LOAI_RV = (string?)noidung.Element("MA_LOAI_RV") ?? "",
                                        GHI_CHU = ReplaceCData(((string?)noidung.Element("GHI_CHU") ?? "")),
                                        NGAY_TTOAN = GetLong(noidung.Element("NGAY_TTOAN")) != 0 ? ConvertCompactTimestampToDateTime(GetLong(noidung.Element("NGAY_TTOAN"))) : ConvertCompactTimestampToDateTime(180001010000),
                                        T_THUOC = GetInt(noidung.Element("T_THUOC")),
                                        T_VTYT = GetInt(noidung.Element("T_VTYT")),
                                        T_TONGCHI_BV = GetInt(noidung.Element("T_TONGCHI_BV")),
                                        T_TONGCHI_BH = GetInt(noidung.Element("T_TONGCHI_BH")),
                                        T_BNTT = GetInt(noidung.Element("T_BNTT")),
                                        T_BNCCT = GetInt(noidung.Element("T_BNCCT")),
                                        T_BHTT = GetInt(noidung.Element("T_BHTT")),
                                        T_NGUONKHAC = GetInt(noidung.Element("T_NGUONKHAC")),
                                        T_BHTT_GDV = GetInt(noidung.Element("T_BHTT_GDV")),
                                        NAM_QT = (string?)noidung.Element("NAM_QT") ?? "",
                                        THANG_QT = (string?)noidung.Element("THANG_QT") ?? "",
                                        MA_LOAI_KCB = (string?)noidung.Element("MA_LOAI_KCB") ?? "",
                                        MA_KHOA = (string?)noidung.Element("MA_KHOA") ?? "",
                                        MA_CSKCB = (string?)noidung.Element("MA_CSKCB") ?? "",
                                        MA_KHUVUC = (string?)noidung.Element("MA_KHUVUC") ?? "",
                                        CAN_NANG = (string?)noidung.Element("CAN_NANG") ?? "",
                                        CAN_NANG_CON = (string?)noidung.Element("CAN_NANG_CON") ?? "",
                                        NAM_NAM_LIEN_TUC = GetInt(noidung.Element("NAM_NAM_LIEN_TUC")),
                                        NGAY_TAI_KHAM = (string?)noidung.Element("NGAY_TAI_KHAM") ?? "",
                                        MA_HSBA = (string?)noidung.Element("MA_HSBA") ?? "",
                                        MA_TTDV = (string?)noidung.Element("MA_TTDV") ?? "",
                                        DU_PHONG = (string?)noidung.Element("DU_PHONG") ?? "",
                                        CSYTID = csytId
                                    };
                                    // thông tin bệnh nhân
                                    bn = benhNhan;
                                }
                                else if (loai.Equals("XML2"))
                                {
                                    var chiTietThuocXmWrapper = noidung.Element("DSACH_CHI_TIET_THUOC");
                                    var dsChiTietThuocXml = chiTietThuocXmWrapper.Elements("CHI_TIET_THUOC");
                                    foreach (var chiTietThuoc in dsChiTietThuocXml)
                                    {
                                        var thuoc = new XML2
                                        {
                                            STT = GetInt(chiTietThuoc.Element("STT")),
                                            MA_LK = maLK,
                                            MA_THUOC = (string?)chiTietThuoc.Element("MA_THUOC") ?? "",
                                            MA_PP_CHEBIEN = (string?)chiTietThuoc.Element("MA_PP_CHEBIEN") ?? "",
                                            MA_CSKCB_THUOC = (string?)chiTietThuoc.Element("MA_CSKCB_THUOC") ?? "",
                                            MA_NHOM = (string?)chiTietThuoc.Element("MA_NHOM") ?? "",
                                            TEN_THUOC = ReplaceCData(((string?)chiTietThuoc.Element("TEN_THUOC") ?? "")),
                                            DON_VI_TINH = (string?)chiTietThuoc.Element("DON_VI_TINH") ?? "",
                                            HAM_LUONG = ReplaceCData(((string?)chiTietThuoc.Element("HAM_LUONG") ?? "")),
                                            DUONG_DUNG = ReplaceCData(((string?)chiTietThuoc.Element("DUONG_DUNG") ?? "")),
                                            DANG_BAO_CHE = ReplaceCData(((string?)chiTietThuoc.Element("DANG_BAO_CHE") ?? "")),
                                            LIEU_DUNG = ReplaceCData(((string?)chiTietThuoc.Element("LIEU_DUNG") ?? "")),
                                            CACH_DUNG = ReplaceCData(((string?)chiTietThuoc.Element("CACH_DUNG") ?? "")),
                                            SO_DANG_KY = ReplaceCData(((string?)chiTietThuoc.Element("SO_DANG_KY") ?? "")),
                                            TT_THAU = ReplaceCData(((string?)chiTietThuoc.Element("TT_THAU") ?? "")),
                                            PHAM_VI = (string?)chiTietThuoc.Element("PHAM_VI") ?? "",
                                            TYLE_TT_BH = GetInt(chiTietThuoc.Element("TYLE_TT_BH")),
                                            SO_LUONG = GetInt(chiTietThuoc.Element("SO_LUONG")),
                                            DON_GIA = GetInt(chiTietThuoc.Element("DON_GIA")),
                                            THANH_TIEN_BV = GetInt(chiTietThuoc.Element("THANH_TIEN_BV")),
                                            THANH_TIEN_BH = GetInt(chiTietThuoc.Element("THANH_TIEN_BH")),
                                            T_NGUONKHAC_NSNN = GetInt(chiTietThuoc.Element("T_NGUONKHAC_NSNN")),
                                            T_NGUONKHAC_VTNN = GetInt(chiTietThuoc.Element("T_NGUONKHAC_VTNN")),
                                            T_NGUONKHAC_VTTN = GetInt(chiTietThuoc.Element("T_NGUONKHAC_VTTN")),
                                            T_NGUONKHAC_CL = GetInt(chiTietThuoc.Element("T_NGUONKHAC_CL")),
                                            T_NGUONKHAC = GetInt(chiTietThuoc.Element("T_NGUONKHAC")),
                                            MUC_HUONG = GetInt(chiTietThuoc.Element("MUC_HUONG")),
                                            T_BNTT = GetInt(chiTietThuoc.Element("T_BNTT")),
                                            T_BNCCT = GetInt(chiTietThuoc.Element("T_BNCCT")),
                                            T_BHTT = GetInt(chiTietThuoc.Element("T_BHTT")),
                                            MA_KHOA = (string?)chiTietThuoc.Element("MA_KHOA") ?? "",
                                            MA_BAC_SI = (string?)chiTietThuoc.Element("MA_BAC_SI") ?? "",
                                            MA_DICH_VU = (string?)chiTietThuoc.Element("MA_DICH_VU") ?? "",
                                            NGAY_YL = GetLong(chiTietThuoc.Element("NGAY_YL"))!=0 ? ConvertCompactTimestampToDateTime(GetLong(chiTietThuoc.Element("NGAY_YL"))) : ConvertCompactTimestampToDateTime(180001010000),
                                            NGAY_TH_YL = GetLong(chiTietThuoc.Element("NGAY_TH_YL")) != 0 ? ConvertCompactTimestampToDateTime(GetLong(chiTietThuoc.Element("NGAY_TH_YL"))) : ConvertCompactTimestampToDateTime(180001010000),
                                            MA_PTTT = (string?)chiTietThuoc.Element("MA_PTTT") ?? "",
                                            NGUON_CTRA = (string?)chiTietThuoc.Element("NGUON_CTRA") ?? "",
                                            VET_THUONG_TP = (string?)chiTietThuoc.Element("VET_THUONG_TP") ?? "",
                                            DU_PHONG = (string?)chiTietThuoc.Element("DU_PHONG") ?? "",
                                            CSYTID = csytId,
                                        };
                                        dsChiTietThuoc.Add(thuoc);
                                        //await _dbContext.SaveChangesAsync();
                                    }
                                }
                                else if (loai.Equals("XML3"))
                                {
                                    var chiTietDvktXmWrapper = noidung.Element("DSACH_CHI_TIET_DVKT");
                                    var dsChiTietDvktXml = chiTietDvktXmWrapper.Elements("CHI_TIET_DVKT");
                                    foreach (var chiTietDvkt in dsChiTietDvktXml)
                                    {
                                        var dvkt = new XML3
                                        {
                                            STT = GetInt(chiTietDvkt.Element("STT")),
                                            MA_LK = maLK,
                                            MA_DICH_VU = (string?)chiTietDvkt.Element("MA_DICH_VU") ?? "",
                                            MA_PTTT_QT = (string?)chiTietDvkt.Element("MA_PTTT_QT") ?? "",
                                            MA_VAT_TU = (string?)chiTietDvkt.Element("MA_VAT_TU") ?? "",
                                            MA_NHOM = (string?)chiTietDvkt.Element("MA_NHOM") ?? "",
                                            GOI_VTYT = (string?)chiTietDvkt.Element("GOI_VTYT") ?? "",
                                            TEN_VAT_TU = (string?)chiTietDvkt.Element("TEN_VAT_TU") ?? "",
                                            TEN_DICH_VU = ReplaceCData(((string?)chiTietDvkt.Element("TEN_DICH_VU") ?? "")),
                                            MA_XANG_DAU = (string?)chiTietDvkt.Element("MA_XANG_DAU") ?? "",
                                            DON_VI_TINH = (string?)chiTietDvkt.Element("DON_VI_TINH") ?? "",
                                            PHAM_VI = (string?)chiTietDvkt.Element("PHAM_VI") ?? "",
                                            SO_LUONG = GetInt(chiTietDvkt.Element("SO_LUONG")),
                                            DON_GIA_BV = GetInt(chiTietDvkt.Element("DON_GIA_BV")),
                                            DON_GIA_BH = GetInt(chiTietDvkt.Element("DON_GIA_BH")),
                                            TT_THAU = GetInt(chiTietDvkt.Element("TT_THAU")),
                                            TYLE_TT_DV = GetInt(chiTietDvkt.Element("TYLE_TT_DV")),
                                            TYLE_TT_BH = GetInt(chiTietDvkt.Element("TYLE_TT_BH")),
                                            THANH_TIEN_BV = GetInt(chiTietDvkt.Element("THANH_TIEN_BV")),
                                            THANH_TIEN_BH = GetInt(chiTietDvkt.Element("THANH_TIEN_BH")),
                                            T_TRANTT = GetInt(chiTietDvkt.Element("T_TRANTT")),
                                            MUC_HUONG = GetInt(chiTietDvkt.Element("MUC_HUONG")),
                                            T_NGUONKHAC_NSNN = GetInt(chiTietDvkt.Element("T_NGUONKHAC_NSNN")),
                                            T_NGUONKHAC_VTNN = GetInt(chiTietDvkt.Element("T_NGUONKHAC_VTNN")),
                                            T_NGUONKHAC_VTTN = GetInt(chiTietDvkt.Element("T_NGUONKHAC_VTTN")),
                                            T_NGUONKHAC_CL = GetInt(chiTietDvkt.Element("T_NGUONKHAC_CL")),
                                            T_NGUONKHAC = GetInt(chiTietDvkt.Element("T_NGUONKHAC")),
                                            T_BNTT = GetInt(chiTietDvkt.Element("T_BNTT")),
                                            T_BNCCT = GetInt(chiTietDvkt.Element("T_BNCCT")),
                                            T_BHTT = GetInt(chiTietDvkt.Element("T_BHTT")),
                                            MA_KHOA = (string?)chiTietDvkt.Element("MA_KHOA") ?? "",
                                            MA_GIUONG = (string?)chiTietDvkt.Element("MA_GIUONG") ?? "",
                                            MA_BAC_SI = (string?)chiTietDvkt.Element("MA_BAC_SI") ?? "",
                                            NGUOI_THUC_HIEN = (string?)chiTietDvkt.Element("NGUOI_THUC_HIEN") ?? "",
                                            MA_BENH = (string?)chiTietDvkt.Element("MA_BENH") ?? "",
                                            MA_BENH_YHCT = (string?)chiTietDvkt.Element("MA_BENH_YHCT") ?? "",
                                            NGAY_YL = GetLong(chiTietDvkt.Element("NGAY_YL")) !=0 ? ConvertCompactTimestampToDateTime(GetLong(chiTietDvkt.Element("NGAY_YL"))) : ConvertCompactTimestampToDateTime(180001010000),
                                            NGAY_TH_YL = GetLong(chiTietDvkt.Element("NGAY_TH_YL"))!=0 ? ConvertCompactTimestampToDateTime(GetLong(chiTietDvkt.Element("NGAY_TH_YL"))) : ConvertCompactTimestampToDateTime(180001010000),
                                            NGAY_KQ = GetLong(chiTietDvkt.Element("NGAY_KQ")) != 0 ? ConvertCompactTimestampToDateTime(GetLong(chiTietDvkt.Element("NGAY_KQ"))) : ConvertCompactTimestampToDateTime(180001010000),
                                            MA_PTTT = (string?)chiTietDvkt.Element("MA_PTTT") ?? "",
                                            VET_THUONG_TP = (string?)chiTietDvkt.Element("VET_THUONG_TP") ?? "",
                                            PP_VO_CAM = (string?)chiTietDvkt.Element("PP_VO_CAM") ?? "",
                                            VI_TRI_TH_DVKT = (string?)chiTietDvkt.Element("VI_TRI_TH_DVKT") ?? "",
                                            MA_MAY = (string?)chiTietDvkt.Element("MA_MAY") ?? "",
                                            MA_HIEU_SP = (string?)chiTietDvkt.Element("MA_HIEU_SP") ?? "",
                                            TAI_SU_DUNG = (string?)chiTietDvkt.Element("TAI_SU_DUNG") ?? "",
                                            DU_PHONG = (string?)chiTietDvkt.Element("DU_PHONG") ?? "",
                                            CSYTID = csytId,
                                        };
                                        dsDichVuKiThuat.Add(dvkt);
                                        //await _dbContext.SaveChangesAsync();
                                    }
                                }
                        }


                        if (bn.MA_LK != "") _dbContext.xml1.Add(bn);
                        if (dsChiTietThuoc.Count > 0) _dbContext.xml2.AddRange(dsChiTietThuoc);
                        if (dsDichVuKiThuat.Count > 0) _dbContext.xml3.AddRange(dsDichVuKiThuat);

                        await _dbContext.SaveChangesAsync();
                        msg += "\n";
                        msg += $"Thêm mới thành công: Bệnh nhân mã: {bn.MA_LK} Tên: {bn.HO_TEN} Ngày sinh:{bn.NGAY_SINH}";
                        msg += "\n";
                        msg += $"{dsChiTietThuoc.Count} Chi Tiết Thuốc, {dsDichVuKiThuat.Count} Dịch vụ kĩ thuật!";
                        
                    }
                    return Ok(msg);
                }
                catch (XmlException xe)
                {
                    return BadRequest($"Lỗi XML: "+ xe.Message);
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
            var res = inp.Replace("<![CDATA[", "").Replace("]]>", "");
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
    }
}
