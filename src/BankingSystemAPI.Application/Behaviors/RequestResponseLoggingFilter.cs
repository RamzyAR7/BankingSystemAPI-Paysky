using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            string requestPayload;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                requestPayload = JsonSerializer.Serialize(request, options);
            }
            catch (Exception)
            {
                requestPayload = "***UNSERIALIZABLE_REQUEST***";
            }

            _logger.LogDebug("MediatR request received: {RequestType} {Method} {Path} UserId={UserId}",
                typeof(TRequest).Name, method, path, userId);

            PrintColoredIfDev($@"
───────────────────────────────
# [INCOMING MEDIATR REQUEST]
───────────────────────────────
-> Type:        {typeof(TRequest).Name}
-> Path:        {method} {path}
-> UserId:      {userId}
-> Authenticated:{isAuthenticated}
-> IP:          {ip}
-> Payload:     {MaskSensitiveData(requestPayload)}
───────────────────────────────", ConsoleColor.Cyan);

            try
            {
                var response = await next();

                sw.Stop();

                string responsePayload;
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    responsePayload = JsonSerializer.Serialize(response, options);
                }
                catch (Exception)
                {
                    responsePayload = "***UNSERIALIZABLE_RESPONSE***";
                }

                _logger.LogDebug("MediatR request completed: {RequestType} {ResponseType} ElapsedMs={Elapsed}",
                    typeof(TRequest).Name, typeof(TResponse).Name, sw.ElapsedMilliseconds);

                PrintColoredIfDev($@"
───────────────────────────────
# [OUTGOING MEDIATR RESPONSE]
───────────────────────────────
-> Type:        {typeof(TResponse).Name}
-> Duration:    {sw.ElapsedMilliseconds} ms
-> UserId:      {userId}
-> Payload:     {MaskSensitiveData(responsePayload)}
───────────────────────────────", ConsoleColor.Green);

                // Optional: central timing info (info level)
                _logger.LogInformation(ApiResponseMessages.Infrastructure.RequestTimingLogFormat, method, path, httpContext?.Response?.StatusCode ?? 0, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unhandled exception in MediatR handler for {RequestType} @ {Path}", typeof(TRequest).Name, path);

                PrintColoredIfDev($@"
───────────────────────────────
# [EXCEPTION DURING MEDIATR HANDLING]
───────────────────────────────
-> Type:        {typeof(TRequest).Name}
-> Duration:    {sw.ElapsedMilliseconds} ms
-> UserId:      {userId}
-> Exception:   {ex.Message}
{ex.StackTrace}
───────────────────────────────", ConsoleColor.Red);

                throw;
            }
        }

        private static string? MaskSensitiveData(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var lowered = input.ToLowerInvariant();
            if (lowered.Contains("password") || lowered.Contains("token") ||
                lowered.Contains("authorization") || lowered.Contains("bearer"))
                return "***MASKED***";

            return input.Length > 1200 ? input.Substring(0, 1200) + "..." : input;
        }

        private void PrintColoredIfDev(string text, ConsoleColor color)
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
