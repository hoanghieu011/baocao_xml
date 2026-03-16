namespace API.Common
{
    public class QueryParamSizeLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _maxQuerySize;
        public QueryParamSizeLimitMiddleware(RequestDelegate next, int maxQuerySize)
        {
            _next = next;
            _maxQuerySize = maxQuerySize;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            int totalSize = 0;

            foreach (var key in context.Request.Query.Keys)
            {
                var value = context.Request.Query[key];
                totalSize += System.Text.Encoding.UTF8.GetByteCount(key) + System.Text.Encoding.UTF8.GetByteCount(value);
            }

            if (totalSize > _maxQuerySize)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsync($"Query parameters too large. Max allowed size is {_maxQuerySize} bytes.");
                return;
            }

            await _next(context);
        }
    }
}