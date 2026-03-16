using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.RegularExpressions;

namespace API.Common
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp-mail.outlook.com";
        private readonly int _smtpPort = 587;
        private readonly string _emailFrom = "1111@sinfo-mt.com.vn";
        private readonly string _emailPassword = "Smtvn@3423;";

        public async Task<bool> SendEmailAsync(List<(string Name, string Email)> recipients, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Quản lý nghỉ phép", _emailFrom));

                var validRecipients = recipients
            .Where(r => !string.IsNullOrWhiteSpace(r.Email) && IsValidEmail(r.Email))
            .ToList();

                if (!validRecipients.Any())
                {
                    // Console.WriteLine("⚠️ Không có email hợp lệ để gửi.");
                    return false;
                }

                foreach (var recipient in validRecipients)
                {
                    email.To.Add(new MailboxAddress(recipient.Name, recipient.Email));
                }

                email.Subject = subject;

                var bodyBuilder = new BodyBuilder { TextBody = body };
                email.Body = bodyBuilder.ToMessageBody();

                // using var smtp = new SmtpClient();
                // await smtp.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                // await smtp.AuthenticateAsync(_emailFrom, _emailPassword);
                // await smtp.SendAsync(email);
                // await smtp.DisconnectAsync(true);

                // Console.WriteLine("📧 Email đã được gửi thành công!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi gửi email: {ex.Message}");
                return false;
            }
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}