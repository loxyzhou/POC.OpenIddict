using System.Diagnostics;

namespace APIAuthorizationServer
{
    public class ContentTypeMiddleware
    {
        private readonly RequestDelegate _next;

        public ContentTypeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            await _next(context);
        }
    }
}
