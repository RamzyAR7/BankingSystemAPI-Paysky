using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

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

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                // Log structured message
                _logger.LogInformation("RequestTiming: {Method} {Path} responded {StatusCode} in {Elapsed} ms", method, path, status, elapsed);

                // Console colored output to differentiate timing logs
                var original = Console.ForegroundColor;
                try
                {
                    // Choose color based on duration
                    if (elapsed >= 2000)
                        Console.ForegroundColor = ConsoleColor.Red; // slow
                    else if (elapsed >= 500)
                        Console.ForegroundColor = ConsoleColor.Yellow; // medium
                    else
                        Console.ForegroundColor = ConsoleColor.Magenta; // fast

                    Console.WriteLine($"[Timing] {DateTime.UtcNow:u} - {method} {path} => {status} in {elapsed} ms");
                }
                finally
                {
                    Console.ForegroundColor = original;
                }
            }
        }
    }
}
