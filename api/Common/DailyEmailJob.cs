using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Common;
using API.Models;
using API.Data;
using API.DTO;
using API.Controllers;

public class DailyEmailJob : BackgroundService
{
    private readonly ILogger<DailyEmailJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    public DailyEmailJob(ILogger<DailyEmailJob> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var targetTime = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0); //16h UTC+7

            if (now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            var delay = targetTime - now;
            _logger.LogInformation($"Email thông báo tự động sẽ chạy vào: {targetTime.AddHours(7)}");

            await Task.Delay(delay, stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var nghiPhepService = scope.ServiceProvider.GetRequiredService<NghiPhepService>();

                var dsNhanVien = GetDsNhanVienDuyet(dbContext);

                foreach (var nv in dsNhanVien)
                {
                    int soLuongThongBao = await nghiPhepService.GetSoLuongThongBao(
                        nv.ma_nv,
                        nv.ma_vi_tri,
                        nv.ten_bo_phan,
                        nv.cong_viec
                    );
                    if (soLuongThongBao == 0) continue;
                    string subject;
                    string body;

                    if (nv.ma_nv == "SMTV-0625" || nv.ma_nv == "SMTV-1469" || nv.ma_nv == "SMTV-1534")
                    {
                        subject = "日報承認通知";
                        body = $"こんにちは {nv.ma_nv} - {nv.full_name} さん、\n\n" +
                               $"{soLuongThongBao} 件の休暇申請が処理待ちです。\n" +
                               "システムにアクセスして処理してください: https://phepnamsinfonia.com.vn \n\n" +
                               "よろしくお願いいたします。";
                    }
                    else
                    {
                        subject = "Thông báo duyệt phiếu nghỉ hàng ngày";
                        body = $"Xin chào {nv.ma_nv} - {nv.full_name},\n\n" +
                               $"Bạn có {soLuongThongBao} phiếu nghỉ chờ xử lý.\n" +
                               "Vui lòng truy cập hệ thống tại https://phepnamsinfonia.com.vn để xử lý.\n\n" +
                               "Trân trọng.";
                    }

                    var recipient = new List<(string Name, string Email)>
                    {
                        (nv.full_name, nv.email)
                    };

                    bool success = await emailService.SendEmailAsync(recipient, subject, body);

                    if (success)
                    {
                        _logger.LogInformation($"📧 Email đã được gửi thành công cho nhân viên: {nv.ma_nv}");
                    }
                    else
                    {
                        _logger.LogError($"❌ Gửi email thất bại cho nhân viên: {nv.ma_nv}");
                    }
                }
            }
        }
    }

    private List<NhanVienDuyetDto> GetDsNhanVienDuyet(ApplicationDbContext context)
    {
        var query = from nv in context.nhan_vien
                    join u in context.user on nv.ma_nv equals u.ma_nv
                    where u.role != null && u.role.Contains("xu_ly")
                    join bp in context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                    from bp in bpGroup.DefaultIfEmpty()
                    select new NhanVienDuyetDto
                    {
                        ma_nv = nv.ma_nv,
                        full_name = nv.full_name,
                        ma_vi_tri = nv.ma_vi_tri,
                        ten_bo_phan = bp != null ? bp.ten_bo_phan : string.Empty,
                        cong_viec = nv.cong_viec,
                        email = nv.email
                    };
        return query.ToList();
    }
}