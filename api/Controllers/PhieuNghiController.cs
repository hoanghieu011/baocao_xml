using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.DTO;
using API.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Security;
using System.Runtime.Intrinsics.X86;
using System.Linq.Expressions;
using MySqlConnector;
using API.Common;
using System.Globalization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NghiPhepController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ImageService _imageService;

        public NghiPhepController(ApplicationDbContext context, ImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }
        private async Task<bool> CheckNhanVienXuLyExists(string congViec, string maViTri)
        {
            if (string.IsNullOrWhiteSpace(congViec) || string.IsNullOrWhiteSpace(maViTri))
            {
                return true;
            }

            bool exists = await _context.nhan_vien
                .Where(nv => nv.cong_viec != null && nv.ma_vi_tri != null)
                .AnyAsync(nv => nv.cong_viec.ToLower().Contains(congViec.ToLower()) &&
                                  nv.ma_vi_tri.ToLower() == maViTri.ToLower());

            return exists;
        }

        [Authorize(Roles = "tao_phieu")]
        [HttpPost]
        /*
        Sửa lại API này cho phép input đầu vào có thể thêm IFormFile (hoặc vẫn có thể bỏ trống nếu không có file). Class NghiPhep có thêm 2 trường:
            public string? image_name { get; set; }
            public string? image_url { get; set; }
        image_name sẽ lưu tên file của người dùng upload lên, image_url lưu tên thật của file khi được lưu vào server khi dùng ImageService đã tạo ở trên
        */
        [Authorize(Roles = "tao_phieu")]
        [HttpPost]
        public async Task<IActionResult> CreateNghiPhep([FromForm] NghiPhepDto dto, IFormFile? file)
        {
            var nv_id = User.FindFirst("id_nv").Value;
            var ma_vi_tri = User.FindFirst("ma_vi_tri").Value;
            var ten_bo_phan = User.FindFirst("ten_bo_phan").Value;
            var ma_nv = User.FindFirst("ma_nv").Value;
            var cong_viec = User.FindFirst("cong_viec").Value;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool exists = await _context.nghi_phep
                            .AnyAsync(np => np.ma_nv == ma_nv && np.trang_thai == "3" &&
                            dto.nghi_den >= np.nghi_tu && dto.nghi_tu <= np.nghi_den);

            if (exists)
            {
                return BadRequest(new
                {
                    code = 9,
                    mess = "Nhân viên đã có ngày nghỉ trong khoảng thời gian này."
                });
            }

            if (file != null)
            {
                try
                {
                    var uniqueFileName = await _imageService.SaveImageAsync(file);
                    dto.image_name = file.FileName;
                    dto.image_url = uniqueFileName;
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            var nghiTuDate = dto.nghi_tu;
            var nghiDenDate = dto.nghi_den;
            List<string> ngayNghiList = new List<string>();
            var ngayNghiDict = new Dictionary<string, int>();

            var fixedHolidaysByYear = new Dictionary<int, List<DateTime>>();
            for (int y = nghiTuDate.Year; y <= nghiDenDate.Year; y++)
            {
                fixedHolidaysByYear[y] = await GetFixedHolidaysByYearAsync(y);
            }

            for (var d = nghiTuDate; d <= nghiDenDate; d = d.AddDays(1))
            {
                if (dto.ky_hieu_ly_do != "TS" && dto.ky_hieu_ly_do != "DS")
                {
                    if (dto.nghi_t7 == 1 && d.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }
                    if (dto.nghi_t7 == 0 && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday))
                    {
                        continue;
                    }
                    int currentYear = d.Year;
                    if (fixedHolidaysByYear.ContainsKey(currentYear))
                    {
                        var fixedHolidays = fixedHolidaysByYear[currentYear];
                        if (fixedHolidays.Any(fh => fh == d.Date))
                        {
                            continue;
                        }
                    }
                }
                ngayNghiList.Add(d.ToString("dd/MM/yyyy"));

                var yearKey = d.Year.ToString();
                if (!ngayNghiDict.ContainsKey(yearKey))
                {
                    ngayNghiDict[yearKey] = 0;
                }
                ngayNghiDict[yearKey]++;
            }

            var ngayNghi = string.Join(", ", ngayNghiList);

            if (dto.ky_hieu_ly_do == "H")
            {
                foreach (var entry in ngayNghiDict)
                {
                    var year = entry.Key;
                    var soNgayNghi = entry.Value;

                    var phepTonResponse = await _context.phep_ton
                        .FirstOrDefaultAsync(p => p.ma_nv == ma_nv && p.year == year);

                    if (phepTonResponse == null || phepTonResponse.phep_ton < soNgayNghi)
                    {
                        return BadRequest(new
                        {
                            code = 1,
                            message = $"{year}"
                        });
                    }
                }
            }

            string trang_thai;
            if ((ten_bo_phan == "QC" && ma_vi_tri == "SM") ||
                (ten_bo_phan == "QC" && (ma_vi_tri == "TL" || ma_vi_tri == "EP" || ma_vi_tri == "SP")) ||
                (ten_bo_phan == "PC" && ma_vi_tri == "DM") ||
                (ten_bo_phan == "PC" && (ma_vi_tri == "TL" || ma_vi_tri == "EP" || ma_vi_tri == "SP")) ||
                (ten_bo_phan == "PUR" && ma_vi_tri == "SDM") ||
                (ten_bo_phan == "ACC" && (ma_vi_tri == "TL" || ma_vi_tri == "EP" || ma_vi_tri == "SP")) ||
                (ten_bo_phan == "SALE" && (ma_vi_tri == "SP" || ma_vi_tri == "SV" || ma_vi_tri == "EP")) ||
                (ma_vi_tri == "DM_1") ||
                (ten_bo_phan == "GA-HR" && ma_vi_tri == "DM") ||
                (ma_vi_tri == "MS") ||
                (ten_bo_phan == "QE") ||
                (ten_bo_phan == "PRD" && (ma_vi_tri == "DM_1" || ma_vi_tri == "SM" || ma_vi_tri == "PMTL")) ||
                (ten_bo_phan == "PRD" && (ma_vi_tri == "DTL" || ma_vi_tri == "TL_1" || ma_vi_tri == "WK" || ma_vi_tri == "WKII") && !(await CheckNhanVienXuLyExists(cong_viec, "SV"))) ||
                (ma_vi_tri == "PME" || ma_vi_tri == "PMS"))
            {
                trang_thai = "1";
            }
            else if ((ten_bo_phan == "GA-HR" && (ma_vi_tri == "EP" || ma_vi_tri == "SP" || ma_vi_tri == "TL")) || ma_vi_tri == "GD")
            {
                trang_thai = "2";
            }
            else
            {
                trang_thai = "0";
            }

            var nghiPhep = new NghiPhep
            {
                ma_nv = ma_nv,
                so_ngay_nghi = dto.so_ngay_nghi,
                ky_hieu_ly_do = dto.ky_hieu_ly_do,
                ly_do_nghi_str = dto.ly_do_nghi_str,
                loai_phep_id = dto.loai_phep_id,
                ngay_tao = DateTime.UtcNow,
                nghi_tu = dto.nghi_tu,
                nghi_den = dto.nghi_den,
                ngay_nghi = ngayNghi,
                trang_thai = trang_thai,
                duyet = 1,
                thong_bao = 1,
                image_name = dto.image_name,
                image_url = dto.image_url
            };

            if (dto.ban_giao != "defaut")
            {
                nghiPhep.ban_giao = dto.ban_giao;
            }

            var nhanVienDetails = GetNhanVienDetails(nghiPhep.ma_nv);
            if (nhanVienDetails.Xoa == 1)
            {
                return BadRequest(new { message = "Nhân viên đã bị xóa" });
            }

            _context.nghi_phep.Add(nghiPhep);
            await _context.SaveChangesAsync();

            var result = await SearchNhanVienXuLy(new SearchNhanVienXuLyDto
            {
                bo_phan = nhanVienDetails.BoPhan,
                trang_thai = nghiPhep.trang_thai,
                ma_vi_tri = nhanVienDetails.MaViTri,
                cong_viec = nhanVienDetails.CongViec,
            });

            var nguoiDuyetList = (result as OkObjectResult)?.Value as List<NhanVienXuLyDto>;

            if (nguoiDuyetList != null && nguoiDuyetList.Any())
            {
                var emailService = new EmailService();

                var recipients = nguoiDuyetList.Select(nd => (nd.full_name, nd.email)).ToList();

                string subject;
                string body;

                if (nguoiDuyetList.Any(nd => nd.ma_nv == "SMTV-0625" ||
                                             nd.ma_nv == "SMTV-1469" ||
                                             nd.ma_nv == "SMTV-1534"))
                {
                    subject = "通知: 休暇申請の承認依頼";
                    body = $"新しい休暇申請が処理待ちです。\n\n" +
                           $"従業員 {nhanVienDetails.MaNv} - {nhanVienDetails.FullName} が {dto.nghi_tu:dd/MM/yyyy} から {dto.nghi_den:dd/MM/yyyy} までの休暇申請を作成しました。\n" +
                           $"申請作成時刻: {(nghiPhep.ngay_tao.AddHours(7)):dd/MM/yyyy hh:mm:ss tt}\n\n" +
                           "https://phepnamsinfonia.com.vn にアクセスして処理してください。\n\n" +
                           "よろしくお願いいたします。";
                }
                else
                {
                    subject = "Thông báo: Yêu cầu duyệt phiếu nghỉ phép";
                    body = $"Bạn có đơn xin nghỉ phép mới cần xử lý\n\n" +
                           $"Nhân viên {nhanVienDetails.MaNv} - {nhanVienDetails.FullName} đã tạo một phiếu nghỉ phép từ {dto.nghi_tu:dd/MM/yyyy} đến {dto.nghi_den:dd/MM/yyyy}.\n" +
                           $"Thời gian tạo: {(nghiPhep.ngay_tao.AddHours(7)):dd/MM/yyyy hh:mm:ss tt}\n\n" +
                           "Vui lòng truy cập hệ thống tại https://phepnamsinfonia.com.vn để xử lý.\n\n" +
                           "Trân trọng cảm ơn.\n";
                }

                _ = Task.Run(async () => await emailService.SendEmailAsync(recipients, subject, body));
            }

            return Ok(new { message = "Tạo phiếu nghỉ phép thành công!" + nv_id, nghiPhep });
        }

        private async Task<List<DateTime>> GetFixedHolidaysByYearAsync(int year)
        {
            var holidays = await _context.holiday
                            .Where(h => h.year == year)
                            .OrderBy(h => h.ngay_nghi)
                            .ToListAsync();
            return ((List<Holiday>)holidays)
                      .Select(h => h.ngay_nghi.Date)
                      .ToList();
        }

        [Authorize(Roles = "admin,bao_bp_cao_bo_phan")]
        [HttpPost("quan-ly-phieu")]
        public async Task<IActionResult> DsPhieuAll([FromBody] NghiPhepSearchDto dto)
        {
            var ten_bo_phan = User.FindFirst("ten_bo_phan").Value;

            var query = from np in _context.nghi_phep
                        join nv in _context.nhan_vien on np.ma_nv equals nv.ma_nv
                        where nv.xoa != 1
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                        from bp in bpGroup.DefaultIfEmpty()
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtGroup
                        from vt in vtGroup.DefaultIfEmpty()
                        join ld in _context.ly_do_nghi on np.ky_hieu_ly_do equals ld.ky_hieu
                        join pt in _context.phep_ton on nv.ma_nv equals pt.ma_nv
                        where pt.year == np.ngay_tao.Year.ToString()
                        select new NghiPhepResultDto
                        {
                            Id = np.id,
                            SoNgayNghi = np.so_ngay_nghi,
                            BanGiao = np.ban_giao,
                            TrangThai = np.trang_thai,
                            NvXuLy1 = np.nv_xu_ly_1,
                            NvXuLy2 = np.nv_xu_ly_2,
                            NvXuLy3 = np.nv_xu_ly_3,
                            NgayTao = np.ngay_tao,
                            NgayXuLy1 = np.ngay_xu_ly_1,
                            NgayXuLy2 = np.ngay_xu_ly_2,
                            NgayXuLy3 = np.ngay_xu_ly_3,
                            LoaiPhepId = np.loai_phep_id,
                            NghiTu = np.nghi_tu,
                            NghiDen = np.nghi_den,
                            NgayNghi = np.ngay_nghi,
                            KyHieuLyDo = np.ky_hieu_ly_do,
                            LyDoNghiStr = np.ly_do_nghi_str,
                            Duyet = np.duyet,
                            FullName = nv.full_name,
                            CongViec = nv.cong_viec,
                            LyDoDienGiai = ld.dien_giai,
                            MaNv = np.ma_nv,
                            TenBoPhan = bp.ten_bo_phan,
                            TenViTri = vt.ten_vi_tri,
                            LyDoTuChoi = np.ly_do_tu_choi,
                            MaNvHuy = np.nv_huy,
                            LyDoHuy = np.ly_do_huy,
                            NgayHuy = np.ngay_huy,
                            PhepTon = pt.phep_ton,
                            ImageName = np.image_name,
                            ImageUrl = _imageService.GetImageUrl(np.image_url, Request)
                        };
            if (!string.IsNullOrEmpty(dto.searchTerm) && dto.searchTerm != "All")
            {
                string searchTermLower = dto.searchTerm.ToLower();
                query = query.Where(x => x.FullName.ToLower().Contains(searchTermLower) ||
                                    x.MaNv.ToLower().Contains(searchTermLower) ||
                                    x.TenBoPhan.ToLower().Contains(searchTermLower));
            }
            // !mainString.Contains(subString)
            if (ten_bo_phan != "GA-HR")
            {
                query = query.Where(x => x.TenBoPhan == ten_bo_phan);
            }
            if (dto.trang_thai == "Chưa xử lý")
            {
                query = query.Where(x => x.TrangThai != "3" && x.TrangThai != "-1" && x.Duyet == 1);
            }
            else if (dto.trang_thai == "Đã duyệt")
            {
                query = query.Where(x => x.TrangThai == "3");
            }
            else if (dto.trang_thai == "Từ chối")
            {
                query = query.Where(x => x.Duyet == 0);
            }
            else if (dto.trang_thai == "Đã hủy")
            {
                query = query.Where(x => x.TrangThai == "-1");
            }
            query = query.OrderByDescending(x => x.Id);
            var totalCount = query.Count();
            var items = query.Skip((dto.Page - 1) * dto.PageSize).Take(dto.PageSize).ToList();
            return Ok(new { Items = items, TotalCount = totalCount });
        }

        [Authorize(Roles = "tao_phieu")]
        [HttpPost("huy-phieu")]
        public async Task<IActionResult> HuyPhieu([FromBody] HuyPhieuRequest request)
        {
            var ma_nv = User.FindFirst("ma_nv").Value;
            var ten_bo_phan = User.FindFirst("ten_bo_phan").Value;
            var nghiPhep = await _context.nghi_phep.FindAsync(request.id);

            if (nghiPhep == null)
            {
                return NotFound(new { message = "Phiếu nghỉ phép không tồn tại." });
            }

            if ((nghiPhep.trang_thai == "3" || nghiPhep.duyet == 0) && nghiPhep.ma_nv == ma_nv && ten_bo_phan != "GA-HR")
            {

                return BadRequest(new { message = "Phiếu đã được xử lý không thể hủy.", nghiPhep });
            }
            if (nghiPhep.ma_nv != ma_nv)
            {
                nghiPhep.nv_huy = ma_nv;
            }

            if (nghiPhep.ky_hieu_ly_do == "H")
            {
                var ngayNghiList = nghiPhep.ngay_nghi.Split(", ").Select(date => DateTime.ParseExact(date, "dd/MM/yyyy", null)).ToList();

                var soNgayTheoNam = ngayNghiList
                    .GroupBy(d => d.Year)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                foreach (var item in soNgayTheoNam)
                {
                    var phepTon = await _context.phep_ton
                        .FirstOrDefaultAsync(pt => pt.ma_nv == nghiPhep.ma_nv && pt.year == item.Key);

                    if (phepTon != null)
                    {
                        phepTon.phep_ton += item.Value;
                    }
                }
            }

            nghiPhep.trang_thai = "-1";
            nghiPhep.ngay_huy = DateTime.UtcNow;
            nghiPhep.ly_do_huy = request.ly_do_huy;
            nghiPhep.thong_bao = 1;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Phiếu nghỉ phép đã được hủy thành công." });
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> SearchDuyetNghiPhep([FromQuery] NghiPhepSearchDto searchDto)
        {
            try
            {
                var ma_nv = User.FindFirst("ma_nv").Value;
                var maViTri = User.FindFirst("ma_vi_tri")?.Value;
                var tenBoPhan = User.FindFirst("ten_bo_phan")?.Value;
                var congViec = User.FindFirst("cong_viec")?.Value;
                if (searchDto.trang_thai == "Đã xử lý")
                {
                    return await GetNghiPhepList(searchDto, ma_nv);
                }

                List<PhieuNghiFilter> phieuNghiFilters = new List<PhieuNghiFilter>();

                switch (tenBoPhan)
                {
                    case "QE":
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var accFilter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "QE" },
                                MaViTriTaoPhieu = new List<string> { "TL", "EP", "SP" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(accFilter);
                        }
                        break;
                    case "ACC":
                        if (maViTri == "DM_1")
                        {
                            var accFilter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(accFilter);
                        }
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            // var accFilter_pho_phong = new PhieuNghiFilter
                            // {
                            //     TrangThaiPhieu = "1",
                            //     TenBoPhanPhieu = new List<string> { "ACC" },
                            //     MaViTriTaoPhieu = new List<string> { "DM_1" },
                            //     CongViecNvTaoPhieu = null
                            // };
                            // phieuNghiFilters.Add(accFilter_pho_phong);
                        }
                        break;
                    case "GA-HR":
                        if (maViTri == "DM")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "All" },
                                MaViTriTaoPhieu = new List<string> { "GD" },
                                CongViecNvTaoPhieu = null
                            });
                            var filter_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "SM", "SDM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_1);

                            var filter_2 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2);

                            var filter_3 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "SM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_3);

                            var filter_4 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PUR" },
                                MaViTriTaoPhieu = new List<string> { "SDM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_4);

                            var filter_5 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "SM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_5);

                            var filter_6 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "SALE" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_6);
                            var filter_7 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "All" },
                                MaViTriTaoPhieu = new List<string> { "MS" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_7);

                            // Duyệt mức cuối
                            var filter_2_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_1);

                            var filter_2_2 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "All" },
                                MaViTriTaoPhieu = new List<string> { "DM_1" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_2);

                            var filter_2_3 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "GA-HR" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_3);

                            var filter_2_4 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "TL", "EP", "SP", "DTL", "TL_1", "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_4);

                            var filter_2_5 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "PMTL", "PME", "PMS", "DTL", "TL_1", "SV", "TTL", "TS", "TE", "TF", "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_5);

                            var filter_2_6 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PUR" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_6);

                            var filter_2_7 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1", "TL", "EP", "SP", "WK", "WKII", "SV" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_7);

                            var filter_2_8 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "SALE" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "SV" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_8);

                            var filter_2_9 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "QE" },
                                MaViTriTaoPhieu = new List<string> { "TL", "EP", "SP" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2_9);
                        }
                        break;
                    case "PC":
                        if (maViTri == "DM_1" || maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1", "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "TL", "SP", "EP" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        else if (maViTri == "TL")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        else if (maViTri == "TL_1")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        break;
                    case "PRD":
                        // if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        // {
                        //     var filter = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "1",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "TL", "TL_1", "DTL", "EP", "SP", "WK", "WKII", "SV", "DM_1" },
                        //         CongViecNvTaoPhieu = null
                        //     };
                        //     phieuNghiFilters.Add(filter);
                        // }
                        // if (maViTri == "DM_1")
                        // {
                        //     // check lại công việc kỹ thuật
                        //     var filter = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "0",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "TL" },
                        //         CongViecNvTaoPhieu = "Kỹ thuật"
                        //     };
                        //     phieuNghiFilters.Add(filter);

                        //     var filter_2 = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "1",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "TL_1", "DTL", "WK", "WKII", "SV" },
                        //         CongViecNvTaoPhieu = null
                        //     };
                        //     phieuNghiFilters.Add(filter_2);
                        // }
                        // if (maViTri == "SV")
                        // {
                        //     var filter = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "0",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "TL_1", "DTL" },
                        //         CongViecNvTaoPhieu = congViec
                        //     };
                        //     phieuNghiFilters.Add(filter);
                        // }
                        // if (maViTri == "TL")
                        // {
                        //     var filter = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "0",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "EP", "SP" },
                        //         CongViecNvTaoPhieu = congViec
                        //     };
                        //     phieuNghiFilters.Add(filter);
                        // }
                        // if (maViTri == "TL_1")
                        // {
                        //     var filter = new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "0",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                        //         CongViecNvTaoPhieu = congViec
                        //     };
                        //     phieuNghiFilters.Add(filter);
                        // }
                        // break;

                        if (maViTri == "DM_1")
                        {
                            var filler = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "SV", "TTL" },
                                CongViecNvTaoPhieu = congViec
                            };
                            phieuNghiFilters.Add(filler);

                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1", "TE", "TS", "TF" },
                                CongViecNvTaoPhieu = congViec
                            });
                        }
                        // Bỏ qua bước duyệt của nhóm trưởng sản xuất - phiếu nv/chuyên viên sx
                        // if (maViTri == "PMTL")
                        // {
                        //     phieuNghiFilters.Add(new PhieuNghiFilter
                        //     {
                        //         TrangThaiPhieu = "0",
                        //         TenBoPhanPhieu = new List<string> { "PRD" },
                        //         MaViTriTaoPhieu = new List<string> { "PME", "PMS" },
                        //         CongViecNvTaoPhieu = null
                        //     });
                        // }
                        if (maViTri == "SV")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1" },
                                CongViecNvTaoPhieu = congViec,
                            });
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                                CongViecNvTaoPhieu = congViec
                            });
                        }
                        if (maViTri == "TL_1")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                                CongViecNvTaoPhieu = congViec
                            });
                            if (!(await CheckNhanVienXuLyExists(congViec, "SV")))
                            {
                                phieuNghiFilters.Add(new PhieuNghiFilter
                                {
                                    TrangThaiPhieu = "1",
                                    TenBoPhanPhieu = new List<string> { "PRD" },
                                    MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                                    CongViecNvTaoPhieu = congViec
                                });
                            }
                        }
                        if (maViTri == "TTL")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "TE", "TS", "TF" },
                                CongViecNvTaoPhieu = null
                            });
                        }
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "PMTL", "PME", "PMS", "SV", "TTL", "DM_1" },
                                CongViecNvTaoPhieu = null
                            });
                        }
                        break;
                    case "PUR":
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "PUR" },
                                MaViTriTaoPhieu = new List<string> { "DM_1", "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        if (maViTri == "DM_1")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "PUR" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        break;
                    case "QC":
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "DM_1", "EP", "SP", "TL", "SV" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                            var filer_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "QE" },
                                MaViTriTaoPhieu = new List<string> { "EP", "SP", "TL" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filer_1);
                        }
                        if (maViTri == "DM_1")
                        {
                            phieuNghiFilters.Add(new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "SV" },
                                CongViecNvTaoPhieu = null
                            });
                        }
                        if (maViTri == "DM_1" || maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1", "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        if (maViTri == "SV")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "DTL", "TL_1" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);

                            var filter_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "WK", "WKII" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_1);
                        }
                        break;
                    // end QC
                    case "SALE":
                        if (maViTri == "SM" || maViTri == "SDM" || maViTri == "DM")
                        {
                            var filter = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "SALE" },
                                MaViTriTaoPhieu = new List<string> { "DM_1", "EP", "SP", "SV" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter);
                        }
                        break;
                    default:
                        if (maViTri == "GD")
                        {
                            var filter_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "SM", "SDM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_1);

                            var filter_2 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PC" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2);

                            var filter_3 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PRD" },
                                MaViTriTaoPhieu = new List<string> { "SM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_3);

                            var filter_4 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "PUR" },
                                MaViTriTaoPhieu = new List<string> { "SDM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_4);

                            var filter_5 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "QC" },
                                MaViTriTaoPhieu = new List<string> { "SM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_5);

                            var filter_6 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "SALE" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_6);

                            var filter_7 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "GA-HR" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_7);
                            var filter_8 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "2",
                                TenBoPhanPhieu = new List<string> { "All" },
                                MaViTriTaoPhieu = new List<string> { "MS" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_8);
                        }
                        if (maViTri == "MS")
                        {
                            var filter_1 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "SM", "SDM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_1);
                            var filter_2 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "0",
                                TenBoPhanPhieu = new List<string> { "SALE" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_2);
                            var filter_3 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "GA-HR" },
                                MaViTriTaoPhieu = new List<string> { "DM" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filter_3);
                            var filer_4 = new PhieuNghiFilter
                            {
                                TrangThaiPhieu = "1",
                                TenBoPhanPhieu = new List<string> { "ACC" },
                                MaViTriTaoPhieu = new List<string> { "DM_1" },
                                CongViecNvTaoPhieu = null
                            };
                            phieuNghiFilters.Add(filer_4);
                        }
                        break;
                }
                // end switch

                var result = new List<NghiPhepResultDto>();
                var rs = new List<NghiPhep>();
                if (phieuNghiFilters?.Any() == true)
                {
                    foreach (var filter in phieuNghiFilters)
                    {
                        bool isAllDepartments = filter.TenBoPhanPhieu.Contains("All");

                        var query = @"
                                        SELECT np.*, 
                                            nv.ma_nv AS nhan_vien_ma_nv, nv.full_name, 
                                            bp.id AS bo_phan_id, bp.ten_bo_phan, 
                                            vt.ma_vi_tri, vt.ten_vi_tri
                                        FROM nghi_phep np
                                        JOIN nhan_vien nv ON np.ma_nv = nv.ma_nv
                                        LEFT JOIN bo_phan bp ON nv.bo_phan_id = bp.id
                                        JOIN vi_tri vt ON nv.ma_vi_tri = vt.ma_vi_tri
                                        WHERE np.trang_thai = @TrangThaiPhieu AND nv.xoa != 1"
                                        + (isAllDepartments ? "" : " AND FIND_IN_SET(bp.ten_bo_phan, @TenBoPhanPhieu) > 0")
                                        + @" AND FIND_IN_SET(vt.ma_vi_tri, @MaViTriTaoPhieu) > 0";
                        // AND (TRIM(@CongViecNvTaoPhieu) LIKE CONCAT('%', TRIM(nv.cong_viec), '%') OR nv.cong_viec IS NULL OR @CongViecNvTaoPhieu IS NULL)";
                        var parameters = new List<MySqlParameter>
                        {
                            new MySqlParameter("@TrangThaiPhieu", filter.TrangThaiPhieu),
                            new MySqlParameter("@MaViTriTaoPhieu", string.Join(",", filter.MaViTriTaoPhieu)),
                            // new MySqlParameter("@CongViecNvTaoPhieu", (object?)filter.CongViecNvTaoPhieu?.Trim() ?? DBNull.Value)
                        };

                        if (!isAllDepartments)
                        {
                            parameters.Add(new MySqlParameter("@TenBoPhanPhieu", string.Join(",", filter.TenBoPhanPhieu)));
                        }

                        var filteredResults = await _context.nghi_phep.FromSqlRaw(query, parameters.ToArray()).ToListAsync();

                        var userTasks = filter.CongViecNvTaoPhieu?
                                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => x.Trim())
                                    .ToList();

                        rs.AddRange(filteredResults);

                        var nvIds = filteredResults.Select(r => r.ma_nv).ToList();
                        foreach (var npRecord in filteredResults)
                        {
                            var nhanVienDetails = GetNhanVienDetails(npRecord.ma_nv, npRecord.ngay_tao.Year.ToString());
                            var ld = GetLyDo(npRecord.ky_hieu_ly_do);
                            if (npRecord != null && nhanVienDetails != null && IsTaskMatch(nhanVienDetails.CongViec, userTasks))
                            {
                                var npWithDetailsDto = new NghiPhepResultDto
                                {
                                    Id = npRecord.id,
                                    SoNgayNghi = npRecord.so_ngay_nghi,
                                    BanGiao = npRecord.ban_giao,
                                    TrangThai = npRecord.trang_thai,
                                    NvXuLy1 = npRecord.nv_xu_ly_1,
                                    NvXuLy2 = npRecord.nv_xu_ly_2,
                                    NvXuLy3 = npRecord.nv_xu_ly_3,
                                    NgayTao = npRecord.ngay_tao,
                                    NgayXuLy1 = npRecord.ngay_xu_ly_1,
                                    NgayXuLy2 = npRecord.ngay_xu_ly_2,
                                    NgayXuLy3 = npRecord.ngay_xu_ly_3,
                                    LoaiPhepId = npRecord.loai_phep_id,
                                    NghiTu = npRecord.nghi_tu,
                                    NghiDen = npRecord.nghi_den,
                                    NgayNghi = npRecord.ngay_nghi,
                                    KyHieuLyDo = npRecord.ky_hieu_ly_do,
                                    LyDoNghiStr = npRecord.ly_do_nghi_str,
                                    Duyet = npRecord.duyet,
                                    MaNv = nhanVienDetails.MaNv,
                                    FullName = nhanVienDetails.FullName,
                                    CongViec = nhanVienDetails.CongViec,
                                    TenBoPhan = nhanVienDetails.BoPhan,
                                    LyDoDienGiai = ld?.dien_giai,
                                    TenViTri = nhanVienDetails.ViTri,
                                    LyDoTuChoi = npRecord.ly_do_tu_choi,
                                    MaNvHuy = npRecord.nv_huy,
                                    LyDoHuy = npRecord.ly_do_huy,
                                    NgayHuy = npRecord.ngay_huy,
                                    Tier = npRecord.duyet == 1 ? 1 : 0,
                                    PhepTon = nhanVienDetails.PhepTon,
                                    ImageName = npRecord.image_name,
                                    ImageUrl = _imageService.GetImageUrl(npRecord.image_url, Request)
                                };

                                result.Add(npWithDetailsDto);
                            }
                        }
                    }
                }

                var result_fn = new List<NghiPhepResultDto>();
                if (searchDto.trang_thai == "Tất cả")
                {
                    var nghiPhepListResult = await GetNghiPhepList(new NghiPhepSearchDto
                    {
                        trang_thai = "Tất cả",
                        Page = 1,
                        PageSize = 90000
                    }, ma_nv) as OkObjectResult;

                    if (nghiPhepListResult != null)
                    {
                        var nghiPhepList = nghiPhepListResult.Value as dynamic;
                        var items = nghiPhepList?.Items as List<NghiPhepResultDto>;

                        if (items != null)
                        {
                            result_fn = result.Concat(items).ToList();
                        }
                        else
                        {
                            result_fn = result;
                        }
                    }
                    else
                    {
                        result_fn = result;
                    }
                }
                else if (searchDto.trang_thai == "Chưa xử lý")
                {
                    foreach (var np in result)
                    {
                        if (np.Duyet == 1)
                        {
                            result_fn.Add(np);
                        }
                    }
                }
                else if (searchDto.trang_thai == "Từ chối")
                {
                    foreach (var np in result)
                    {
                        if (np.Duyet == 0 && check_nv_xl(np, ma_nv) != 0 && !check_nv_xl_upper(np, ma_nv))
                        {
                            np.Tier = 0;
                            result_fn.Add(np);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(searchDto.searchTerm) && searchDto.searchTerm != "All")
                {
                    string lowerSearchTerm = searchDto.searchTerm.ToLower();
                    result_fn = result_fn.Where(r =>
                        r.FullName.ToLower().Contains(lowerSearchTerm) ||
                        r.TenBoPhan.ToLower().Contains(lowerSearchTerm) ||
                        r.MaNv.ToLower().Contains(lowerSearchTerm)
                    ).ToList();
                }
                result_fn = result_fn.OrderByDescending(np => np.Tier ?? 0)
                     .ThenByDescending(np => np.Id)
                     .ToList();

                var totalCount = result_fn.Count;
                var skip = (searchDto.Page - 1) * searchDto.PageSize;
                var paginatedResult = result_fn.Skip(skip).Take(searchDto.PageSize);

                return Ok(new { TotalCount = totalCount, Items = paginatedResult });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        private bool IsTaskMatch(string nvCongViec, List<string> userTasks)
        {
            if (string.IsNullOrWhiteSpace(nvCongViec) || userTasks == null)
                return true;

            var nvItems = nvCongViec
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            return nvItems.All(item => userTasks != null && userTasks.Contains(item));
        }
        private async Task<IActionResult> GetNghiPhepList(NghiPhepSearchDto dto, string ma_nv_xl)
        {
            // if (dto.trang_thai != "Đã xử lý")
            // {
            //     return BadRequest("Chỉ có thể lấy danh sách phiếu nghỉ đã xử lý.");
            // }

            var query = from np in _context.nghi_phep
                        join nv in _context.nhan_vien on np.ma_nv equals nv.ma_nv
                        where nv.xoa != 1
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                        from bp in bpGroup.DefaultIfEmpty()
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtGroup
                        from vt in vtGroup.DefaultIfEmpty()
                        join ld in _context.ly_do_nghi on np.ky_hieu_ly_do equals ld.ky_hieu
                        join pt in _context.phep_ton on nv.ma_nv equals pt.ma_nv
                        where (np.nv_xu_ly_1 == ma_nv_xl || np.nv_xu_ly_2 == ma_nv_xl || np.nv_xu_ly_3 == ma_nv_xl)
                                && pt.year == np.ngay_tao.Year.ToString()
                        select new
                        {
                            np.id,
                            np.so_ngay_nghi,
                            np.ban_giao,
                            np.trang_thai,
                            np.nv_xu_ly_1,
                            np.nv_xu_ly_2,
                            np.nv_xu_ly_3,
                            np.ngay_tao,
                            np.ngay_xu_ly_1,
                            np.ngay_xu_ly_2,
                            np.ngay_xu_ly_3,
                            np.loai_phep_id,
                            np.nghi_tu,
                            np.nghi_den,
                            np.ngay_nghi,
                            np.ky_hieu_ly_do,
                            np.ly_do_nghi_str,
                            np.duyet,
                            nv.full_name,
                            nv.cong_viec,
                            ld.dien_giai,
                            np.ma_nv,
                            bp.ten_bo_phan,
                            vt.ten_vi_tri,
                            np.ly_do_tu_choi,
                            np.nv_huy,
                            np.ly_do_huy,
                            np.ngay_huy,
                            pt.phep_ton,
                            np.image_name,
                            image_url = _imageService.GetImageUrl(np.image_url, Request)
                        };

            if (!string.IsNullOrEmpty(dto.searchTerm) && dto.searchTerm != "All")
            {
                string searchTermLower = dto.searchTerm.ToLower();
                query = query.Where(x => x.full_name.ToLower().Contains(searchTermLower) ||
                                    x.ma_nv.ToLower().Contains(searchTermLower) ||
                                    x.ten_bo_phan.ToLower().Contains(searchTermLower));
            }
            var result = new List<NghiPhepResultDto>();
            foreach (var rs in query)
            {
                var add = new NghiPhepResultDto
                {
                    Id = rs.id,
                    SoNgayNghi = rs.so_ngay_nghi,
                    BanGiao = rs.ban_giao,
                    TrangThai = rs.trang_thai,
                    NvXuLy1 = rs.nv_xu_ly_1,
                    NvXuLy2 = rs.nv_xu_ly_2,
                    NvXuLy3 = rs.nv_xu_ly_3,
                    NgayTao = rs.ngay_tao,
                    NgayXuLy1 = rs.ngay_xu_ly_1,
                    NgayXuLy2 = rs.ngay_xu_ly_2,
                    NgayXuLy3 = rs.ngay_xu_ly_3,
                    LoaiPhepId = rs.loai_phep_id,
                    NghiTu = rs.nghi_tu,
                    NghiDen = rs.nghi_den,
                    NgayNghi = rs.ngay_nghi,
                    KyHieuLyDo = rs.ky_hieu_ly_do,
                    LyDoNghiStr = rs.ly_do_nghi_str,
                    Duyet = rs.duyet,
                    MaNv = rs.ma_nv,
                    FullName = rs.full_name,
                    TenBoPhan = rs.ten_bo_phan,
                    LyDoDienGiai = rs.dien_giai,
                    CongViec = rs.cong_viec,
                    TenViTri = rs.ten_vi_tri,
                    LyDoTuChoi = rs.ly_do_tu_choi,
                    MaNvHuy = rs.nv_huy,
                    LyDoHuy = rs.ly_do_huy,
                    NgayHuy = rs.ngay_huy,
                    Tier = 0,
                    PhepTon = rs.phep_ton,
                    ImageName = rs.image_name,
                    ImageUrl = rs.image_url
                };
                if (rs.duyet == 1)
                {
                    result.Add(add);
                }
                else
                {
                    if (check_nv_xl_upper(add, ma_nv_xl))
                    {
                        result.Add(add);
                    }
                }
            }
            result = result.OrderByDescending(x => x.Id).ToList();
            var totalCount = result.Count();
            var items = result.Skip((dto.Page - 1) * dto.PageSize).Take(dto.PageSize).ToList();

            return Ok(new { Items = items, TotalCount = totalCount });
        }
        private async Task<IActionResult> SearchNhanVienXuLy([FromQuery] SearchNhanVienXuLyDto dto)
        {
            if (dto.cong_viec == "defaut") dto.cong_viec = null;
            var nhanVienFilter = new List<NhanVienXuLyFilter>();
            if (dto.ma_vi_tri == "GD")
            {
                if (dto.trang_thai == "2")
                {
                    var filer = new NhanVienXuLyFilter
                    {
                        TenBoPhan = "GA-HR",
                        MaViTri = new List<string> { "DM" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filer);
                }
            }
            if (dto.ma_vi_tri == "DM_1" || dto.ma_vi_tri == "SV")
            {
                if (dto.trang_thai == "1" && dto.bo_phan != "ACC")
                {
                    var filter = new NhanVienXuLyFilter
                    {
                        TenBoPhan = dto.bo_phan,
                        MaViTri = new List<string> { "SM", "SDM", "DM" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filter);
                }
                else if (dto.trang_thai == "1" && dto.bo_phan == "ACC")
                {
                    var filter = new NhanVienXuLyFilter
                    {
                        TenBoPhan = null,
                        MaViTri = new List<string> { "MS" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filter);
                }
                else if (dto.trang_thai == "2")
                {
                    var filter = new NhanVienXuLyFilter
                    {
                        TenBoPhan = "GA-HR",
                        MaViTri = new List<string> { "DM" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filter);
                }
            }
            if (dto.ma_vi_tri == "MS")
            {
                if (dto.trang_thai == "1")
                {
                    var filter = new NhanVienXuLyFilter
                    {
                        TenBoPhan = "GA-HR",
                        MaViTri = new List<string> { "DM" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filter);
                }
                if (dto.trang_thai == "2")
                {
                    var filter = new NhanVienXuLyFilter
                    {
                        TenBoPhan = null,
                        MaViTri = new List<string> { "GD" },
                        CongViec = null
                    };
                    nhanVienFilter.Add(filter);
                }
            }
            switch (dto.bo_phan)
            {
                case "ACC":
                    if (dto.ma_vi_tri == "SM" || dto.ma_vi_tri == "SDM")
                    {
                        if (dto.trang_thai == "0")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "MS" },
                                CongViec = null
                            });
                        }
                        else if (dto.trang_thai == "1")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.trang_thai == "2")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "TL")
                    {
                        if (dto.trang_thai == "1")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1" },
                                CongViec = null
                            });
                        }
                        else if (dto.trang_thai == "2")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    break;
                case "GA-HR":
                    if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "DM")
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        else if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "TL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "1" && dto.ma_vi_tri == "DM")
                    {
                        nhanVienFilter.Add(new NhanVienXuLyFilter
                        {
                            TenBoPhan = null,
                            MaViTri = new List<string> { "MS" },
                            CongViec = null
                        });
                    }
                    break;
                case "PC":
                    if (dto.trang_thai == "0")
                    {
                        if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "TL" },
                                CongViec = null
                            });
                        }
                        if (dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "TL_1" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "1")
                    {
                        if (dto.ma_vi_tri == "DM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "TL" || dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1", "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "DM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "TL" || dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" ||
                                 dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "WK" ||
                                 dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    break;
                case "PRD":
                    // if (dto.trang_thai == "0")
                    // {
                    //     if (dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "DTL")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "SV" },
                    //             CongViec = dto.cong_viec
                    //         });
                    //     }
                    //     if (dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "TL_1" },
                    //             CongViec = dto.cong_viec
                    //         });
                    //     }
                    //     // check lại nhóm trưởng kỹ thuật
                    //     if (dto.ma_vi_tri == "TL")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "DM_1" },
                    //             CongViec = "Kỹ thuật"
                    //         });
                    //     }
                    //     // check lại nhân viên/chuyên viên kỹ thuật
                    //     if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "TL" },
                    //             CongViec = "Kỹ thuật"
                    //         });
                    //     }
                    // }
                    // else if (dto.trang_thai == "1")
                    // {
                    //     if (dto.ma_vi_tri == "SM")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = "GA-HR",
                    //             MaViTri = new List<string> { "DM" },
                    //             CongViec = null
                    //         });
                    //     }
                    //     // check lại phó phòng theo công việc
                    //     if (dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "SM", "SDM", "DM" },
                    //             CongViec = null
                    //         });
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "DM_1" },
                    //             CongViec = null
                    //         });
                    //     }
                    //     if (dto.ma_vi_tri == "TL" || dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = dto.bo_phan,
                    //             MaViTri = new List<string> { "SM", "SDM", "DM" },
                    //             CongViec = null
                    //         });
                    //     }
                    // }
                    // else if (dto.trang_thai == "2")
                    // {
                    //     if (dto.ma_vi_tri == "SM")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = null,
                    //             MaViTri = new List<string> { "GD" },
                    //             CongViec = null
                    //         });
                    //     }
                    //     else if (dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "WK" | dto.ma_vi_tri == "WKII" ||
                    //              dto.ma_vi_tri == "TL" || dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP")
                    //     {
                    //         nhanVienFilter.Add(new NhanVienXuLyFilter
                    //         {
                    //             TenBoPhan = "GA-HR",
                    //             MaViTri = new List<string> { "DM" },
                    //             CongViec = null
                    //         });
                    //     }
                    // }

                    if (dto.trang_thai == "0")
                    {
                        // if (dto.ma_vi_tri == "PME" || dto.ma_vi_tri == "PMS")
                        // {
                        //     nhanVienFilter.Add(new NhanVienXuLyFilter
                        //     {
                        //         TenBoPhan = dto.bo_phan,
                        //         MaViTri = new List<string> { "PMTL" },
                        //         CongViec = null
                        //     });
                        // }
                        if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "PRD",
                                MaViTri = new List<string> { "SV" },
                                CongViec = dto.cong_viec
                            });
                        }
                        else if (dto.ma_vi_tri == "SV" || dto.ma_vi_tri == "TTL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1" },
                                CongViec = dto.cong_viec
                            });
                        }
                        else if (dto.ma_vi_tri == "TE" || dto.ma_vi_tri == "TS" || dto.ma_vi_tri == "TF")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "TTL" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "TL_1" },
                                CongViec = dto.cong_viec
                            });
                        }
                    }
                    else if (dto.trang_thai == "1")
                    {
                        if (dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SV" },
                                CongViec = dto.cong_viec
                            });
                            if (!(await CheckNhanVienXuLyExists(dto.cong_viec, "SV")))
                            {
                                nhanVienFilter.Add(new NhanVienXuLyFilter
                                {
                                    TenBoPhan = dto.bo_phan,
                                    MaViTri = new List<string> { "TL_1" },
                                    CongViec = dto.cong_viec
                                });
                            }
                        }
                        else if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "TE" ||
                                dto.ma_vi_tri == "TS" || dto.ma_vi_tri == "TF")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1" },
                                CongViec = dto.cong_viec,
                            });
                        }
                        else if (dto.ma_vi_tri == "PMTL" || dto.ma_vi_tri == "PME" || dto.ma_vi_tri == "PMS" ||
                                dto.ma_vi_tri == "SV" || dto.ma_vi_tri == "TTL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "DM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "GA-HR" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "SM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "SM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "PMTL" || dto.ma_vi_tri == "PME" || dto.ma_vi_tri == "PMS" ||
                                dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "SV" ||
                                dto.ma_vi_tri == "TTL" || dto.ma_vi_tri == "TE" || dto.ma_vi_tri == "TS" ||
                                dto.ma_vi_tri == "TF" || dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    break;
                case "PUR":
                    if (dto.trang_thai == "0")
                    {
                        if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "TL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "1")
                    {
                        if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "TL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "SDM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "TL")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "SDM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                    }
                    break;
                case "QC":
                    if (dto.trang_thai == "0")
                    {
                        if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SV" },
                                CongViec = null
                            });
                        }
                        if (dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SV" },
                                CongViec = null
                            });
                        }
                        if (dto.ma_vi_tri == "SV")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1" },
                                CongViec = null
                            });
                        }
                    }
                    if (dto.trang_thai == "1")
                    {
                        if (dto.ma_vi_tri == "DTL" || dto.ma_vi_tri == "TL_1" || dto.ma_vi_tri == "WK" || dto.ma_vi_tri == "WKII")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "DM_1", "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "TL" || dto.ma_vi_tri == "EP" || dto.ma_vi_tri == "SP" || dto.ma_vi_tri == "SV")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                        else if (dto.ma_vi_tri == "SM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "SM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                        else //
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    break;
                // end QC
                case "QE":
                    if (dto.trang_thai == "1")
                    {
                        nhanVienFilter.Add(new NhanVienXuLyFilter
                        {
                            TenBoPhan = "QE",
                            MaViTri = new List<string> { "SM", "SDM", "DM" },
                            CongViec = null
                        });
                        nhanVienFilter.Add(new NhanVienXuLyFilter
                        {
                            TenBoPhan = "QC",
                            MaViTri = new List<string> { "SM", "SDM", "DM" },
                            CongViec = null
                        });
                    }
                    else if (dto.trang_thai == "2")
                    {
                        nhanVienFilter.Add(new NhanVienXuLyFilter
                        {
                            TenBoPhan = "GA-HR",
                            MaViTri = new List<string> { "DM" },
                            CongViec = null
                        });
                    }
                    break;
                case "SALE":
                    if (dto.trang_thai == "0" && dto.ma_vi_tri == "DM")
                    {
                        nhanVienFilter.Add(new NhanVienXuLyFilter
                        {
                            TenBoPhan = null,
                            MaViTri = new List<string> { "MS" },
                            CongViec = null
                        });
                    }
                    else if (dto.trang_thai == "1")
                    {
                        if (dto.ma_vi_tri == "DM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                        else
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = dto.bo_phan,
                                MaViTri = new List<string> { "SM", "SDM", "DM" },
                                CongViec = null
                            });
                        }
                    }
                    else if (dto.trang_thai == "2")
                    {
                        if (dto.ma_vi_tri == "DM")
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = null,
                                MaViTri = new List<string> { "GD" },
                                CongViec = null
                            });
                        }
                        else
                        {
                            nhanVienFilter.Add(new NhanVienXuLyFilter
                            {
                                TenBoPhan = "GA-HR",
                                MaViTri = new List<string> { "DM" },
                                CongViec = null
                            });
                        }
                    }
                    break;
            }
            if (nhanVienFilter.Any())
            {
                var query = from nv in _context.nhan_vien
                            where nv.xoa != 1
                            join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                            from bp in bpGroup.DefaultIfEmpty()
                            join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtGroup
                            from vt in vtGroup.DefaultIfEmpty()
                            select new NhanVienXuLyDto
                            {
                                id = nv.id,
                                ma_nv = nv.ma_nv,
                                full_name = nv.full_name,
                                bo_phan_id = nv.bo_phan_id,
                                tenBoPhan = bp != null ? bp.ten_bo_phan : null,
                                cong_viec = nv.cong_viec,
                                ma_vi_tri = nv.ma_vi_tri,
                                email = nv.email,
                                vi_tri = vt.ten_vi_tri
                            };

                var results = new List<NhanVienXuLyDto>();

                foreach (var filter in nhanVienFilter)
                {
                    var tempQuery = query;

                    if (!string.IsNullOrEmpty(filter.TenBoPhan))
                    {
                        tempQuery = tempQuery.Where(nv => nv.tenBoPhan == filter.TenBoPhan);
                    }

                    tempQuery = tempQuery.Where(nv => filter.MaViTri.Contains(nv.ma_vi_tri));

                    // if (!string.IsNullOrEmpty(filter.CongViec))
                    // {
                    //     tempQuery = tempQuery.Where(nv =>
                    //         (!string.IsNullOrEmpty(filter.CongViec) && nv.cong_viec != null && nv.cong_viec.Contains(filter.CongViec))
                    //         || (nv.cong_viec == null && dto.trang_thai == "0"));
                    // }
                    if (!string.IsNullOrEmpty(filter.CongViec))
                    {
                        // 1. Tách các phần
                        var parts = filter.CongViec
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

                        // 2. Lọc những NV có cong_viec != null và phải chứa đủ từng part
                        var withJobs = tempQuery.Where(nv => nv.cong_viec != null);
                        foreach (var part in parts)
                        {
                            // EF Core sẽ dịch thành "AND nv.cong_viec LIKE '%{part}%'"
                            string pattern = $"%{part}%";
                            withJobs = withJobs.Where(nv => EF.Functions.Like(nv.cong_viec, pattern));
                        }

                        // 3. Lọc thêm những NV chưa có cong_viec nhưng vẫn cho phép với trang_thai == "0"
                        var noJobs = tempQuery
                            .Where(nv => nv.cong_viec == null && dto.trang_thai == "0");

                        // 4. Kết hợp 2 tập kết quả (dùng Concat hoặc Union tuỳ nhu cầu)
                        tempQuery = withJobs.Concat(noJobs);
                    }



                    results.AddRange(await tempQuery.ToListAsync());
                }

                return Ok(results.GroupBy(r => r.ma_nv).Select(g => g.First()).ToList());
            }
            return Ok(new List<NhanVienXuLyDto>());
        }
        [Authorize(Roles = "tao_phieu")]
        [HttpGet("searchCN")]
        public async Task<IActionResult> ListNpCn([FromQuery] NghiPhepSearchDto dto)
        {
            var ma_nv = User.FindFirst("ma_nv").Value;
            var query = from np in _context.nghi_phep
                        join nv in _context.nhan_vien on np.ma_nv equals nv.ma_nv
                        where nv.xoa != 1
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bps
                        from bp in bps.DefaultIfEmpty()
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri
                        join ld in _context.ly_do_nghi on np.ky_hieu_ly_do equals ld.ky_hieu
                        where np.ma_nv == ma_nv
                        select new NghiPhepResultDto
                        {
                            Id = np.id,
                            SoNgayNghi = np.so_ngay_nghi,
                            BanGiao = np.ban_giao,
                            TrangThai = np.trang_thai,
                            NvXuLy1 = np.nv_xu_ly_1,
                            NvXuLy2 = np.nv_xu_ly_2,
                            NvXuLy3 = np.nv_xu_ly_3,
                            NgayTao = np.ngay_tao,
                            NgayXuLy1 = np.ngay_xu_ly_1,
                            NgayXuLy2 = np.ngay_xu_ly_2,
                            NgayXuLy3 = np.ngay_xu_ly_3,
                            LoaiPhepId = np.loai_phep_id,
                            NghiTu = np.nghi_tu,
                            NghiDen = np.nghi_den,
                            NgayNghi = np.ngay_nghi,
                            KyHieuLyDo = np.ky_hieu_ly_do,
                            LyDoNghiStr = np.ly_do_nghi_str,
                            Duyet = np.duyet,
                            MaNv = nv.ma_nv,
                            FullName = nv.full_name,
                            CongViec = nv.cong_viec,
                            TenBoPhan = bp.ten_bo_phan,
                            LyDoDienGiai = ld.dien_giai,
                            TenViTri = vt.ten_vi_tri,
                            LyDoTuChoi = np.ly_do_tu_choi,
                            MaNvHuy = np.nv_huy,
                            LyDoHuy = np.ly_do_huy,
                            NgayHuy = np.ngay_huy,
                            ThongBao = np.thong_bao,
                            ImageName = np.image_name,
                            ImageUrl = _imageService.GetImageUrl(np.image_url, Request)
                        };
            if (dto.trang_thai == "Chưa xử lý")
            {
                query = query.Where(x => x.Duyet == 1 && x.TrangThai != "3" && x.TrangThai != "-1");
            }
            else if (dto.trang_thai == "Đã duyệt")
            {
                query = query.Where(x => x.TrangThai == "3");
            }
            else if (dto.trang_thai == "Từ chối")
            {
                query = query.Where(x => x.Duyet == 0);
            }
            else if (dto.trang_thai == "Đã hủy")
            {
                query = query.Where(x => x.TrangThai == "-1");
            }
            query = query.OrderByDescending(x => x.ThongBao)
                         .ThenByDescending(x => x.Id);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((dto.Page - 1) * dto.PageSize).Take(dto.PageSize).ToListAsync();

            return Ok(new { Items = items, TotalCount = totalCount });
        }

        [Authorize(Roles = "tao_phieu")]
        [HttpGet("thong-bao-cn")]
        public async Task<IActionResult> GetNumThongBaoCn()
        {
            var ma_nv = User.FindFirst("ma_nv").Value;
            var query = from np in _context.nghi_phep
                        join nv in _context.nhan_vien on np.ma_nv equals nv.ma_nv
                        where nv.xoa != 1
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri
                        join ld in _context.ly_do_nghi on np.ky_hieu_ly_do equals ld.ky_hieu
                        where np.ma_nv == ma_nv && np.thong_bao == 1
                        select new
                        {
                            Id = np.id,
                        };
            var totalCount = await query.CountAsync();

            return Ok(new { TotalCount = totalCount });
        }
        [Authorize(Roles = "tao_phieu")]
        [HttpPost("cap-nhat-thong-bao")]
        public async Task<IActionResult> UpdateThongBao([FromBody] ulong id)
        {
            var nghiPhep = await _context.nghi_phep.FindAsync(id);
            if (nghiPhep == null)
            {
                return NotFound(new { message = "Không tìm thấy phiếu nghỉ" });
            }

            nghiPhep.thong_bao = 0;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông báo thành công" });
        }

        private int check_nv_xl(NghiPhepResultDto nprs, string ma_nv)
        {
            if (nprs.NvXuLy1 == ma_nv) return 1;
            if (nprs.NvXuLy2 == ma_nv) return 2;
            if (nprs.NvXuLy3 == ma_nv) return 3;
            return 0;
        }
        private int check_lv_xl(NghiPhepResultDto nprs, string ma_nv)
        {
            if (nprs.NvXuLy3 != null) return 3;
            if (nprs.NvXuLy2 != null) return 2;
            if (nprs.NvXuLy1 != null) return 1;
            return 0;
        }
        private bool check_nv_xl_upper(NghiPhepResultDto nprs, string ma_nv)
        {
            var lv_nv_xl = check_nv_xl(nprs, ma_nv);
            var lv_xl = check_lv_xl(nprs, ma_nv);
            if (lv_xl > lv_nv_xl) return true;
            return false;
        }
        private NhanVienDetailsDto GetNhanVienDetails(string ma_nv, string year)
        {
            var query = from nv in _context.nhan_vien
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bps
                        from bp in bps.DefaultIfEmpty()
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vts
                        from vt in vts.DefaultIfEmpty()
                        join pt in _context.phep_ton on nv.ma_nv equals pt.ma_nv into pts
                        from pt in pts.DefaultIfEmpty()
                        where nv.ma_nv == ma_nv && pt.year == year
                        select new NhanVienDetailsDto
                        {
                            Id = nv.id,
                            MaNv = nv.ma_nv,
                            FullName = nv.full_name,
                            GioiTinh = nv.gioi_tinh,
                            CongViec = nv.cong_viec,
                            BoPhan = bp != null ? bp.ten_bo_phan : null,
                            MaViTri = nv.ma_vi_tri,
                            ViTri = vt != null ? vt.ten_vi_tri : null,
                            Email = nv.email,
                            Xoa = nv.xoa,
                            PhepTon = pt.phep_ton
                        };

            NhanVienDetailsDto nhanVienDetails = query.FirstOrDefault();

            return nhanVienDetails;
        }
        private NhanVienDetailsDto GetNhanVienDetails(string ma_nv)
        {
            var query = from nv in _context.nhan_vien
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bps
                        from bp in bps.DefaultIfEmpty()
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vts
                        from vt in vts.DefaultIfEmpty()
                        where nv.ma_nv == ma_nv
                        select new NhanVienDetailsDto
                        {
                            Id = nv.id,
                            MaNv = nv.ma_nv,
                            FullName = nv.full_name,
                            GioiTinh = nv.gioi_tinh,
                            CongViec = nv.cong_viec,
                            BoPhan = bp != null ? bp.ten_bo_phan : null,
                            MaViTri = nv.ma_vi_tri,
                            ViTri = vt != null ? vt.ten_vi_tri : null,
                            Email = nv.email,
                            Xoa = nv.xoa
                        };

            NhanVienDetailsDto nhanVienDetails = query.FirstOrDefault();

            return nhanVienDetails;
        }
        [HttpGet("search_lydo")]
        public LyDoNghi GetLyDo(string ky_hieu)
        {
            var query = from ld in _context.ly_do_nghi
                        where ld.ky_hieu == ky_hieu
                        select new LyDoNghi(ld.ky_hieu, ld.dien_giai);
            LyDoNghi rs = query.FirstOrDefault();
            return rs;
        }

        [Authorize]
        [HttpPut("batch-update-status")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusDto dto)
        {
            var ma_nv = User.FindFirst("ma_nv").Value;
            if (dto.Ids == null || dto.Ids.Count == 0 || string.IsNullOrEmpty(dto.TrangThai))
            {
                return BadRequest("Invalid data. Please provide both IDs and a new status.");
            }

            var recordsToUpdate = await _context.nghi_phep
                .Where(np => dto.Ids.Contains(np.id))
                .ToListAsync();

            if (recordsToUpdate.Count == 0)
            {
                return NotFound("Không tìm thấy phiếu nghỉ cần cập nhật.");
            }
            int updatedCount = 0;
            for (int i = 0; i < dto.Ids.Count; i++)
            {
                var record = recordsToUpdate.FirstOrDefault(r => r.id == dto.Ids[i]);
                var check_np_nam = true;
                if (record != null && record.ky_hieu_ly_do == "H")
                {
                    // Lấy thông tin mã nhân viên từ bảng nhan_vien
                    var ma_nv_xl = await _context.nhan_vien
                        .Where(nv => nv.ma_nv == record.ma_nv)
                        .Select(nv => nv.ma_nv)
                        .FirstOrDefaultAsync();

                    if (ma_nv_xl != null)
                    {
                        // Tách các ngày nghỉ thành danh sách
                        var ngayNghiList = record.ngay_nghi
                            .Split(',')
                            .Select(d => DateTime.ParseExact(d.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                            .ToList();
                        // Nhóm theo năm và tính số ngày nghỉ theo từng năm
                        var ngayNghiByYear = ngayNghiList.GroupBy(d => d.Year)
                            .ToDictionary(g => g.Key.ToString(), g => g.Count());

                        foreach (var item in ngayNghiByYear)
                        {
                            var year = item.Key;
                            var soNgayNghi = item.Value;

                            // Lấy số ngày phép tồn của năm đó
                            var phepTon = await _context.phep_ton
                                .Where(pt => pt.ma_nv == ma_nv_xl && pt.year == year)
                                .Select(pt => pt.phep_ton)
                                .FirstOrDefaultAsync();

                            if (soNgayNghi > phepTon)
                            {
                                check_np_nam = false;
                                break;
                            }
                        }
                    }
                }
                if (record == null || record.trang_thai != dto.PreTrangThai[i])
                {
                    continue;
                }
                if (record.duyet == 1 && dto.TrangThai == "Duyệt" && !check_np_nam)
                {
                    if (recordsToUpdate.Count > 1)
                    {
                        continue;
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            code = 2,
                            message = $"Nhân viên đã hết số ngày nghỉ phép năm"
                        });
                    }

                }
                var ma_nv_xin = record.ma_nv;
                var nhanVienDetails = GetNhanVienDetails(ma_nv_xin);

                if (record.duyet == 1)
                {
                    if (dto.TrangThai == "Duyệt")
                    {
                        bool exists = await _context.nghi_phep
                                    .AnyAsync(np => np.ma_nv == record.ma_nv && np.trang_thai == "3" &&
                                    record.nghi_den >= np.nghi_tu && record.nghi_tu <= np.nghi_den);

                        if (exists)
                        {
                            if (recordsToUpdate.Count > 1)
                            {
                                continue;
                            }
                            else
                            {
                                return BadRequest(new
                                {
                                    code = 9,
                                    mess = "Nhân viên đã có ngày nghỉ trong khoảng thời gian này."
                                });
                            }
                        }
                        switch (record.trang_thai)
                        {
                            case "0":
                                record.thong_bao = 1;
                                record.trang_thai = "1";
                                record.ngay_xu_ly_1 = DateTime.UtcNow;
                                record.nv_xu_ly_1 = ma_nv;
                                break;
                            case "1":
                                record.thong_bao = 1;
                                record.trang_thai = "2";
                                record.ngay_xu_ly_2 = DateTime.UtcNow;
                                record.nv_xu_ly_2 = ma_nv;
                                break;
                            case "2":
                                record.thong_bao = 1;
                                record.trang_thai = "3";
                                record.ngay_xu_ly_3 = DateTime.UtcNow;
                                record.nv_xu_ly_3 = ma_nv;

                                if (record.ky_hieu_ly_do == "H")
                                {
                                    var ngayNghiList = record.ngay_nghi
                                        .Split(',')
                                        .Select(d => DateTime.ParseExact(d.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture))
                                        .ToList();

                                    var maNhanVien = _context.nhan_vien
                                        .Where(nv => nv.ma_nv == record.ma_nv)
                                        .Select(nv => nv.ma_nv)
                                        .FirstOrDefault();

                                    if (maNhanVien != null)
                                    {
                                        var ngayNghiTheoNam = ngayNghiList
                                            .GroupBy(d => d.Year)
                                            .ToDictionary(g => g.Key, g => g.Count());

                                        foreach (var item in ngayNghiTheoNam)
                                        {
                                            int nam = item.Key;
                                            int soNgayNghi = item.Value;

                                            var phepTon = _context.phep_ton
                                                .Where(p => p.ma_nv == maNhanVien && Convert.ToInt32(p.year) == nam)
                                                .FirstOrDefault();

                                            if (phepTon != null)
                                            {
                                                if (phepTon.phep_ton >= soNgayNghi)
                                                {
                                                    phepTon.phep_ton -= soNgayNghi;
                                                }

                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        // Gửi người duyệt
                        var result = await SearchNhanVienXuLy(new SearchNhanVienXuLyDto
                        {
                            bo_phan = nhanVienDetails.BoPhan,
                            trang_thai = record.trang_thai,
                            ma_vi_tri = nhanVienDetails.MaViTri,
                            cong_viec = nhanVienDetails.CongViec,
                        });
                        var nguoiDuyetList = (result as OkObjectResult)?.Value as List<NhanVienXuLyDto>;
                        if (nguoiDuyetList != null && nguoiDuyetList.Any())
                        {
                            var emailService = new EmailService();
                            var nhanVien_CXL = GetNhanVienDetails(ma_nv);
                            var recipients = nguoiDuyetList.Select(nd => (nd.full_name, nd.email)).ToList();
                            string subject;
                            string body;

                            if (nguoiDuyetList.Any(nd => nd.ma_nv == "SMTV-0625" ||
                                                         nd.ma_nv == "SMTV-1469" ||
                                                         nd.ma_nv == "SMTV-1534"))
                            {
                                subject = "通知: 休暇申請承認依頼";
                                body = $"新しい休暇申請が処理待ちです。\n\n" +
                                       $"従業員 {nhanVienDetails.MaNv} - {nhanVienDetails.FullName} が休暇申請を作成しました。\n" +
                                       $"担当者: {nhanVien_CXL.MaNv} - {nhanVien_CXL.FullName}\n\n" +
                                       "システムにアクセスして https://phepnamsinfonia.com.vn/ で処理してください。\n\n" +
                                       "よろしくお願いいたします。\n";
                            }
                            else
                            {
                                subject = "Thông báo: Yêu cầu duyệt phiếu nghỉ phép";
                                body = $"Bạn có đơn xin nghỉ phép mới cần xử lý\n\n" +
                                       $"Nhân viên {nhanVienDetails.MaNv} - {nhanVienDetails.FullName} đã tạo một phiếu nghỉ phép.\n" +
                                       $"Cán bộ chuyển xử lý: {nhanVien_CXL.MaNv} - {nhanVien_CXL.FullName}\n\n" +
                                       "Vui lòng truy cập hệ thống tại https://phepnamsinfonia.com.vn/ để xử lý.\n\n" +
                                       "Trân trọng cảm ơn.\n";
                            }

                            _ = Task.Run(async () => await emailService.SendEmailAsync(recipients, subject, body));


                        }
                        // Gửi người tạo
                        var emailService_1 = new EmailService();
                        var nhanVien_CXL_1 = GetNhanVienDetails(ma_nv);

                        var recipients_1 = new List<(string Name, string Email)>
                        {
                            (nhanVienDetails.FullName, nhanVienDetails.Email),
                        };

                        string subject_1;
                        string tt_nv_xl;
                        string line_1;
                        string body_1;

                        // Kiểm tra xem nhanVienDetails.MaNv có thuộc danh sách gửi tiếng Nhật hay không
                        bool sendJapanese = (nhanVienDetails.MaNv == "SMTV-0625" ||
                                             nhanVienDetails.MaNv == "SMTV-1469" ||
                                             nhanVienDetails.MaNv == "SMTV-1534");

                        if (sendJapanese)
                        {
                            if (record.trang_thai == "3")
                            {
                                subject_1 = "通知: 休暇申請が承認されました";
                                tt_nv_xl = $"処理担当者: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                                line_1 = "休暇申請が承認されました\n\n";
                            }
                            else
                            {
                                subject_1 = "通知: 休暇申請が転送されました";
                                tt_nv_xl = $"処理担当者: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                                line_1 = "休暇申請が転送されました\n\n";
                            }

                            body_1 = line_1 +
                                     $"従業員 {nhanVienDetails.MaNv} - {nhanVienDetails.FullName}\n" +
                                     $"休暇期間: {record.nghi_tu:dd/MM/yyyy} から {record.nghi_den:dd/MM/yyyy}\n" +
                                     tt_nv_xl +
                                     "詳細は https://phepnamsinfonia.com.vn にアクセスしてご確認ください。\n\n" +
                                     "よろしくお願いいたします。\n";
                        }
                        else
                        {
                            if (record.trang_thai == "3")
                            {
                                subject_1 = "Thông báo: Yêu cầu nghỉ phép đã được duyệt";
                                tt_nv_xl = $"Cán bộ xử lý: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                                line_1 = "Yêu cầu nghỉ phép đã được duyệt\n\n";
                            }
                            else
                            {
                                subject_1 = "Thông báo: Yêu cầu nghỉ phép đã được chuyển tiếp";
                                tt_nv_xl = $"Cán bộ chuyển xử lý: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                                line_1 = "Yêu cầu nghỉ phép đã được chuyển tiếp\n\n";
                            }

                            body_1 = line_1 +
                                     $"Nhân viên {nhanVienDetails.MaNv} - {nhanVienDetails.FullName}.\n" +
                                     $"Nghỉ từ {record.nghi_tu:dd/MM/yyyy} đến {record.nghi_den:dd/MM/yyyy}\n" +
                                     tt_nv_xl +
                                     "Vui lòng truy cập hệ thống tại https://phepnamsinfonia.com.vn để xem chi tiết.\n\n" +
                                     "Trân trọng cảm ơn.\n";
                        }

                        _ = Task.Run(async () => await emailService_1.SendEmailAsync(recipients_1, subject_1, body_1));


                    }
                    else if (dto.TrangThai == "Từ chối")
                    {
                        switch (record.trang_thai)
                        {
                            case "0":
                                record.thong_bao = 1;
                                record.duyet = 0;
                                record.ngay_xu_ly_1 = DateTime.UtcNow;
                                record.nv_xu_ly_1 = ma_nv;
                                break;
                            case "1":
                                record.thong_bao = 1;
                                record.duyet = 0;
                                record.ngay_xu_ly_2 = DateTime.UtcNow;
                                record.nv_xu_ly_2 = ma_nv;
                                break;
                            case "2":
                                record.thong_bao = 1;
                                record.duyet = 0;
                                record.ngay_xu_ly_3 = DateTime.UtcNow;
                                record.nv_xu_ly_3 = ma_nv;
                                break;
                        }
                        record.ly_do_tu_choi = dto.LyDoTuChoi;
                        // Gửi người tạo
                        var emailService_1 = new EmailService();
                        var nhanVien_CXL_1 = GetNhanVienDetails(ma_nv);

                        var recipients_1 = new List<(string Name, string Email)>
                        {
                            (nhanVienDetails.FullName, nhanVienDetails.Email),
                        };
                        string subject_1;
                        string tt_nv_xl;
                        string line_1;
                        string body_1;

                        bool sendJapanese = (nhanVienDetails.MaNv == "SMTV-0625" ||
                                             nhanVienDetails.MaNv == "SMTV-1469" ||
                                             nhanVienDetails.MaNv == "SMTV-1534");

                        if (sendJapanese)
                        {
                            subject_1 = "通知: 休暇申請が却下されました";
                            tt_nv_xl = $"処理担当者: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                            line_1 = $"却下理由: {record.ly_do_tu_choi}\n\n";

                            body_1 = $"従業員 {nhanVienDetails.MaNv} - {nhanVienDetails.FullName}\n" +
                                     $"休暇期間: {record.nghi_tu:dd/MM/yyyy} から {record.nghi_den:dd/MM/yyyy}\n" +
                                     tt_nv_xl + line_1 +
                                     "詳細は https://phepnamsinfonia.com.vn にアクセスしてご確認ください。\n\n" +
                                     "よろしくお願いいたします。\n";
                        }
                        else
                        {
                            subject_1 = "Thông báo: Yêu cầu nghỉ phép đã bị từ chối";
                            tt_nv_xl = $"Cán bộ xử lý: {nhanVien_CXL_1.MaNv} - {nhanVien_CXL_1.FullName}\n\n";
                            line_1 = $"Lý do từ chối: {record.ly_do_tu_choi}\n\n";

                            body_1 = $"Nhân viên {nhanVienDetails.MaNv} - {nhanVienDetails.FullName}.\n" +
                                     $"Nghỉ từ {record.nghi_tu:dd/MM/yyyy} đến {record.nghi_den:dd/MM/yyyy}\n" +
                                     tt_nv_xl + line_1 +
                                     "Vui lòng truy cập hệ thống tại https://phepnamsinfonia.com.vn để xem chi tiết.\n\n" +
                                     "Trân trọng cảm ơn.\n";
                        }

                        _ = Task.Run(async () => await emailService_1.SendEmailAsync(recipients_1, subject_1, body_1));

                    }
                    updatedCount++;
                }
                await _context.SaveChangesAsync();

            }
            // if (updatedCount > 0)
            // {
            //     await _context.SaveChangesAsync();
            // }

            return Ok(new
            {
                Message = $"Cập nhật trạng thái thành công cho {updatedCount} phiếu nghỉ.",
                UpdatedCount = updatedCount
            });
        }
        [Authorize]
        [HttpGet("{maNv}")]
        public async Task<ActionResult<IEnumerable<NghiPhepResult>>> GetNghiPhepInfo(string maNv)
        {
            var currentYear = DateTime.UtcNow.Year;

            var loaiPhepList = await _context.loai_phep.ToListAsync();

            var result = new List<NghiPhepResult>();

            foreach (var loaiPhep in loaiPhepList)
            {
                var soNgayDaNghi = await _context.nghi_phep
                    .Where(np => np.ma_nv == maNv
                                  && np.loai_phep_id == loaiPhep.id
                                  && np.ngay_tao.Year == currentYear
                                  && np.trang_thai == "Đã xử lý")
                    .SumAsync(np => np.so_ngay_nghi);

                var soNgayConLai = loaiPhep.so_ngay_nghi - soNgayDaNghi;

                result.Add(new NghiPhepResult
                {
                    LoaiPhepId = loaiPhep.id,
                    TenLoaiPhep = loaiPhep.ten_loai_phep,
                    SoNgayDaNghi = soNgayDaNghi,
                    SoNgayConLai = soNgayConLai
                });
            }
            return Ok(result);
        }

        [Authorize]
        [HttpGet("quy-trinh/{ma_nv}")]
        public async Task<IActionResult> GetQuyTrinh(string ma_nv)
        {
            NhanVienDetailsDto nhan_vien = GetNhanVienDetails(ma_nv);
            if (nhan_vien == null)
            {
                return NotFound("Nhân viên không tồn tại.");
            }
            var tier_1 = await SearchNhanVienXuLy(new SearchNhanVienXuLyDto
            {
                bo_phan = nhan_vien.BoPhan,
                trang_thai = "0",
                ma_vi_tri = nhan_vien.MaViTri,
                cong_viec = nhan_vien.CongViec
            });
            var tier_1_list = (tier_1 as OkObjectResult)?.Value as List<NhanVienXuLyDto>;
            var tier_2 = await SearchNhanVienXuLy(new SearchNhanVienXuLyDto
            {
                bo_phan = nhan_vien.BoPhan,
                trang_thai = "1",
                ma_vi_tri = nhan_vien.MaViTri,
                cong_viec = nhan_vien.CongViec
            });
            var tier_2_list = (tier_2 as OkObjectResult)?.Value as List<NhanVienXuLyDto>;
            var tier_3 = await SearchNhanVienXuLy(new SearchNhanVienXuLyDto
            {
                bo_phan = nhan_vien.BoPhan,
                trang_thai = "2",
                ma_vi_tri = nhan_vien.MaViTri,
                cong_viec = nhan_vien.CongViec
            });
            var tier_3_list = (tier_3 as OkObjectResult)?.Value as List<NhanVienXuLyDto>;
            // Console.WriteLine("\n\n\n\n" + tier_3_list.Count() + "\n\n\n");

            // Bỏ các trạng thái ảo của phiếu (<)
            if ((nhan_vien.BoPhan == "QC" && nhan_vien.MaViTri == "SM") ||
                (nhan_vien.BoPhan == "QC" && (nhan_vien.MaViTri == "TL" || nhan_vien.MaViTri == "EP" || nhan_vien.MaViTri == "SP")) ||
                (nhan_vien.BoPhan == "PC" && nhan_vien.MaViTri == "DM") ||
                (nhan_vien.BoPhan == "PC" && (nhan_vien.MaViTri == "TL" || nhan_vien.MaViTri == "EP" || nhan_vien.MaViTri == "SP")) ||
                (nhan_vien.BoPhan == "PUR" && nhan_vien.MaViTri == "SDM") ||
                (nhan_vien.BoPhan == "ACC" && (nhan_vien.MaViTri == "TL" || nhan_vien.MaViTri == "EP" || nhan_vien.MaViTri == "SP")) ||
                (nhan_vien.BoPhan == "SALE" && (nhan_vien.MaViTri == "SP" || nhan_vien.MaViTri == "SV" || nhan_vien.MaViTri == "EP")) ||
                (nhan_vien.MaViTri == "DM_1") ||
                (nhan_vien.BoPhan == "GA-HR" && nhan_vien.MaViTri == "DM") ||
                (nhan_vien.MaViTri == "MS") ||
                (nhan_vien.BoPhan == "QE") ||
                // CHỉnh sửa PRD
                (nhan_vien.BoPhan == "PRD" && (nhan_vien.MaViTri == "DM_1" || nhan_vien.MaViTri == "SM" || nhan_vien.MaViTri == "PMTL" || nhan_vien.MaViTri == "PME" || nhan_vien.MaViTri == "PMS")) ||
                (nhan_vien.BoPhan == "PRD" && (nhan_vien.MaViTri == "DTL" || nhan_vien.MaViTri == "TL_1" || nhan_vien.MaViTri == "WK" || nhan_vien.MaViTri == "WKII") && !(await CheckNhanVienXuLyExists(nhan_vien.CongViec, "SV"))))
            {
                tier_1_list = null;
            }
            if ((nhan_vien.BoPhan == "GA-HR" && (nhan_vien.MaViTri == "EP" || nhan_vien.MaViTri == "SP" || nhan_vien.MaViTri == "TL")) || nhan_vien.MaViTri == "GD")
            {
                tier_2_list = null;
                tier_1_list = null;
            }
            return Ok(new
            {
                Tier1 = tier_1_list,
                Tier2 = tier_2_list,
                Tier3 = tier_3_list
            });
        }
    }
    public class NghiPhepResult
    {
        public ulong LoaiPhepId { get; set; }
        public string TenLoaiPhep { get; set; }
        public int SoNgayDaNghi { get; set; }
        public int SoNgayConLai { get; set; }
    }

    public class BatchUpdateStatusDto
    {
        public List<ulong> Ids { get; set; }
        public List<string> PreTrangThai { get; set; }
        public string TrangThai { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
    public class HuyPhieuRequest
    {
        public ulong id { get; set; }
        public string ly_do_huy { get; set; } = string.Empty;
    }

}
