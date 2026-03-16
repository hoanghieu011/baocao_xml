using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace API.Common
{
    public class ImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _imagesFolder;

        private readonly string[] _allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
            _imagesFolder = Path.Combine(_env.WebRootPath, "images");

            if (!Directory.Exists(_imagesFolder))
            {
                Directory.CreateDirectory(_imagesFolder);
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ");
            }

            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !_allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Chỉ chấp nhận các file ảnh (.jpg, .jpeg, .png, .gif)");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(_imagesFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName;
        }

        public string GetImageUrl(string fileName, HttpRequest request)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            return $"{request.Scheme}://{request.Host}/images/{fileName}";
        }
    }
}
