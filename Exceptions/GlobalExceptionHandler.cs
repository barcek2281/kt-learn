using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KT_Learn.Exceptions
{
    /// <summary>
    /// Ловит всё, что вылетело из контроллеров/сервисов, и отдаёт единый ProblemDetails.
    /// Подключается через AddExceptionHandler + app.UseExceptionHandler().
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IHostEnvironment environment,
            ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _environment = environment;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails();

            if (exception is AppException appException)
            {
                // Ожидаемая ошибка бизнес-логики: сообщение писали мы, его можно показать клиенту.
                problemDetails.Status = (int)appException.StatusCode;
                problemDetails.Title = appException.Message;

                _logger.LogWarning("{Exception}: {Message} ({Method} {Path})",
                    appException.GetType().Name, appException.Message,
                    httpContext.Request.Method, httpContext.Request.Path);
            }
            else
            {
                // Неожиданная ошибка: наружу — только общая формулировка, детали в логи.
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Внутренняя ошибка сервера";

                if (_environment.IsDevelopment())
                {
                    problemDetails.Detail = exception.ToString();
                }

                _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });
        }
    }
}
