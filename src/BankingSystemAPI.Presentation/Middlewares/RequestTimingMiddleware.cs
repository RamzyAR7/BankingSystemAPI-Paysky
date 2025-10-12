#region Usings
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Presentation.Middlewares
{
    /// <summary>
    /// Middleware that measures the time taken to process each HTTP request and logs it.
    /// Uses colored console output to stand out from other logs.
    /// </summary>
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger, IHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;
                var method = context.Request.Method;
                var path = context.Request.Path;
                var status = context.Response?.StatusCode;

                // Log structured message using centralized format
                _logger.LogInformation(ApiResponseMessages.Infrastructure.RequestTimingLogFormat, method, path, status, elapsed);

                // Also optionally write colored console output in Development for quick visual feedback
                if (_env.IsDevelopment())
                {
                    var original = Console.ForegroundColor;
                    try
                    {
                        if (elapsed >= 2000)
                            Console.ForegroundColor = ConsoleColor.Red; // slow
                        else if (elapsed >= 500)
                            Console.ForegroundColor = ConsoleColor.Yellow; // medium
                        else
                            Console.ForegroundColor = ConsoleColor.Magenta; // fast

                        Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.RequestTimingConsoleFormat, DateTime.UtcNow, method, path, status, elapsed));
                    }
                    finally
                    {
                        Console.ForegroundColor = original;
                    }
                }
            }
        }
    }
}

