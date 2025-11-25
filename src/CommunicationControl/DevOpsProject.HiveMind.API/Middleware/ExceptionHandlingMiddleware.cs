using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace DevOpsProject.HiveMind.API.Middleware
{
    public class ExceptionHandlingMiddleware : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occured: {Message}", exception.Message);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            var errorResponse = new
            {
                Message = "Unexpected error occured",
                Detail = _hostEnvironment.IsDevelopment() ? exception.ToString() : null
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
            await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);
            return true;
        }
    }
}
