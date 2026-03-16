using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace API.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly string _imagesFolder;

        public ImageController(IWebHostEnvironment env)
        {
            _imagesFolder = Path.Combine(env.WebRootPath, "images");
            if (!Directory.Exists(_imagesFolder))
            {
                Directory.CreateDirectory(_imagesFolder);
            }
        }

        /// <summary>
        /// Xóa các ảnh đã được tạo quá số ngày quy định.
        /// </summary>
        /// <param name="days">Số ngày quy định</param>
        /// <returns>Số lượng file đã bị xóa</returns>
        [HttpDelete("cleanup/{days:int}")]
        public IActionResult DeleteImagesOlderThan(int days)
        {
            int deletedCount = 0;

            if (!Directory.Exists(_imagesFolder))
            {
                return Ok(new { deletedCount });
            }

            var files = Directory.GetFiles(_imagesFolder);
            foreach (var file in files)
            {
                var creationTime = System.IO.File.GetCreationTime(file);
                if (DateTime.Now - creationTime > TimeSpan.FromDays(days))
                {
                    System.IO.File.Delete(file);
                    deletedCount++;
                }
            }
            return Ok(new { deletedCount });
        }
    }
}
