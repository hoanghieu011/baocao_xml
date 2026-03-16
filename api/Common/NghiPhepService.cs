using API.Data;
using API.DTO;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

public class NghiPhepService
{
    private readonly ApplicationDbContext _context;
    public NghiPhepService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetSoLuongThongBao(string ma_nv, string maViTri, string tenBoPhan, string? congViec)
    {
        try
        {
            var searchDto = new NghiPhepSearchDto
            {
                trang_thai = "Chưa xử lý",
                searchTerm = "All",
                Page = 1,
                PageSize = 1
            };
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
                        var nhanVienDetails = GetNhanVienDetails(npRecord.ma_nv);
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
                                LyDoDienGiai = "",
                                TenViTri = nhanVienDetails.ViTri,
                                LyDoTuChoi = npRecord.ly_do_tu_choi,
                                MaNvHuy = npRecord.nv_huy,
                                LyDoHuy = npRecord.ly_do_huy,
                                NgayHuy = npRecord.ngay_huy,
                                Tier = npRecord.duyet == 1 ? 1 : 0
                            };

                            result.Add(npWithDetailsDto);
                        }
                    }
                }
            }

            var result_fn = new List<NghiPhepResultDto>();
            if (searchDto.trang_thai == "Chưa xử lý")
            {
                foreach (var np in result)
                {
                    if (np.Duyet == 1)
                    {
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

            return totalCount;
        }
        catch (Exception ex)
        {
            return 0;
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
                        Email = nv.email
                    };

        NhanVienDetailsDto nhanVienDetails = query.FirstOrDefault();

        return nhanVienDetails;
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
}
