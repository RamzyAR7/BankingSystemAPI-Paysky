using System;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Microsoft.AspNetCore.Http;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Behaviors
{
    public class RequestResponseLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _env;

        public RequestResponseLoggingBehavior(
            ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> logger,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment env)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var httpContext = _httpContextAccessor.HttpContext;

            var method = httpContext?.Request?.Method ?? "N/A";
            var path = httpContext?.Request?.Path.Value ?? "N/A";
            var user = httpContext?.User;
            var userId = user?.FindFirst("uid")?.Value ?? user?.Identity?.Name ?? "Anonymous";
            var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();

            // Request ID
            var requestId = httpContext?.Request?.Headers[LoggingConstants.RequestIdHeader].ToString();
            if (string.IsNullOrWhiteSpace(requestId))
                requestId = Guid.NewGuid().ToString();

            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            using (LogContext.PushProperty(LoggingConstants.RequestIdProperty, requestId))
            using (LogContext.PushProperty(LoggingConstants.TraceIdProperty, traceId))
            {
                _logger.LogInformation("Incoming {RequestType} {Method} {Path} UserId={UserId} Authenticated={Authenticated} IP={IP}",
                    typeof(TRequest).Name, method, path, userId, isAuthenticated, ip);

                if (_env.IsDevelopment())
                {
                    var requestPayload = SafeSerialize(request);
                    PrintColored($@"
───────────────────────────────
# [INCOMING MEDIATR REQUEST]
───────────────────────────────
-> Type: {typeof(TRequest).Name}
-> Path: {method} {path}
-> UserId: {userId}
-> Authenticated: {isAuthenticated}
-> IP: {ip}
-> Payload: {MaskSensitiveData(requestPayload)}
───────────────────────────────", ConsoleColor.Cyan);
                }

                try
                {
                    var response = await next();
                    sw.Stop();

                    if (_env.IsDevelopment())
                    {
                        var responsePayload = SafeSerialize(response);

                        PrintColored($@"
───────────────────────────────
# [OUTGOING MEDIATR RESPONSE]
───────────────────────────────
-> Type: {typeof(TResponse).Name}
-> Duration: {sw.ElapsedMilliseconds} ms
-> UserId: {userId}
-> Payload: {MaskSensitiveData(responsePayload)}
───────────────────────────────", ConsoleColor.Green);

                        _logger.LogDebug("Response {ResponseType} DurationMs={Duration} Payload={Payload}",
                            typeof(TResponse).Name, sw.ElapsedMilliseconds, MaskSensitiveData(responsePayload));
                    }
                    else
                    {
                        _logger.LogInformation("Handled {RequestType} DurationMs={Duration} UserId={UserId} Path={Path}",
                            typeof(TRequest).Name, sw.ElapsedMilliseconds, userId, path);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    sw.Stop();

                    if (_env.IsDevelopment())
                    {
                        PrintColored($@"
───────────────────────────────
# [EXCEPTION DURING MEDIATR HANDLING]
───────────────────────────────
-> Type: {typeof(TRequest).Name}
-> Duration: {sw.ElapsedMilliseconds} ms
-> UserId: {userId}
-> Exception: {ex.Message}
{ex.StackTrace}
───────────────────────────────", ConsoleColor.Red);
                    }

                    _logger.LogError(ex, "Exception during handling {RequestType} Path={Path} UserId={UserId} DurationMs={Duration}",
                        typeof(TRequest).Name, path, userId, sw.ElapsedMilliseconds);

                    throw;
                }
            }
        }

        private static string SafeSerialize(object? obj)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(obj, options);
            }
            catch
            {
                return "***UNSERIALIZABLE_OBJECT***";
            }
        }

        private static string MaskSensitiveData(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? "";

            // Regexes for sensitive fields
            var patterns = new[]
            {
                "\"password\"\\s*:\\s*\".*?\"",
                "\"token\"\\s*:\\s*\".*?\"",
                "\"authorization\"\\s*:\\s*\".*?\"",
                "\"bearer\"\\s*:\\s*\".*?\"",
                "\"secret\"\\s*:\\s*\".*?\"",
                "\"pin\"\\s*:\\s*\".*?\""
            };

            foreach (var pattern in patterns)
                input = Regex.Replace(input, pattern, "\"***MASKED***\"", RegexOptions.IgnoreCase);

            return input.Length > 1200 ? input.Substring(0, 1200) + "..." : input;
        }

        private void PrintColored(string text, ConsoleColor color)
        {
            if (_env.IsDevelopment())
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = old;
            }
        }
    }
}
