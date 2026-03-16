using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
namespace API.Common
{
    public class RequestSizeLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _maxBodySize;

        public RequestSizeLimitMiddleware(RequestDelegate next, int maxBodySize)
        {
            _next = next;
            _maxBodySize = maxBodySize;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxBodySize)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsync($"Payload too large. Max allowed size is {_maxBodySize} bytes.");
                return;
            }

            await _next(context);
        }
    }
}