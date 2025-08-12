using System.Net;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace TaskManagerAPI.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next; //ссылка на следующий middleware в цепочке
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при обработке запроса: {Path}", context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; //HTTP-статус

            //формат ответа
            var response = new
            {
                message = ex.Message,
                detail = _env.IsDevelopment() ? ex.StackTrace : null
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
