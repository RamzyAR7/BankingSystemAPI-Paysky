#region Usings
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
#endregion


namespace BankingSystemAPI.Infrastructure.Jobs
{
    public class RefreshTokenCleanupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupJob> _logger;

        public RefreshTokenCleanupJob(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Core run logic that performs cleanup once. Exceptions propagate to caller for centralized handling.
        /// </summary>
        public async Task RunOnceAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cyan = "\u001b[36m";
            var reset = "\u001b[0m";

            var scanMsg = string.Format("{0}[CleanupJob] Scanning for expired/inactive refresh tokens...{1}", cyan, reset);
            Console.WriteLine(scanMsg);

            const int batchSize = 100; // Tune as needed for your DB
            int totalTokens = 0;
            int removedTokens = 0;
            int batchNumber = 0;
            var jobStart = DateTime.UtcNow;

            // Query only expired/inactive tokens from DB
            // Use direct DateTime comparisons so EF can translate the query (computed properties like IsExpired are not mapped)
            var now = DateTime.UtcNow;
            var expiredTokensQuery = context.Set<RefreshToken>()
                .Where(rt => rt.RevokedOn != null || rt.ExpiresOn <= now || rt.AbsoluteExpiresOn <= now);
            var expiredTokensList = await expiredTokensQuery.ToListAsync(cancellationToken);
            totalTokens = expiredTokensList.Count;

            for (int i = 0; i < expiredTokensList.Count; i += batchSize)
            {
                var batch = expiredTokensList.Skip(i).Take(batchSize).ToList();
                batchNumber++;
                var batchStart = DateTime.UtcNow;
                int batchRemoved = 0;
                foreach (var token in batch)
                {
                    try
                    {
                        context.Remove(token);
                        batchRemoved++;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Retry logic: try up to 3 times
                        int retries = 0;
                        bool success = false;
                        while (retries < 3 && !success)
                        {
                            retries++;
                            try
                            {
                                await Task.Delay(100 * retries, cancellationToken); // Exponential backoff small
                                context.Remove(token);
                                batchRemoved++;
                                success = true;
                            }
                            catch (DbUpdateConcurrencyException)
                            {
                                // continue retrying
                            }
                        }
                    }
                }

                // Commit after each batch
                await context.SaveChangesAsync(cancellationToken);

                removedTokens += batchRemoved;
                var batchEnd = DateTime.UtcNow;
                _logger.LogInformation(ApiResponseMessages.Logging.CleanupJobBatch, batchNumber, batchRemoved, (batchEnd - batchStart).TotalSeconds);
            }

            var jobEnd = DateTime.UtcNow;
            var foundMsg = string.Format("{0}[CleanupJob] Found {1} tokens to clean.{2}", cyan, totalTokens, reset);
            Console.WriteLine(foundMsg);
            var removedMsg = string.Format("{0}[CleanupJob] Removed {1} expired/invalid refresh tokens.{2}", cyan, removedTokens, reset);
            Console.WriteLine(removedMsg);
            _logger.LogInformation(ApiResponseMessages.Logging.CleanupJobRunCompleted, totalTokens, removedTokens, (jobEnd - jobStart).TotalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxRetries = 5;
            int attempt = 0;

            // Run continuously; apply retry/backoff on errors to make job resilient
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);

                    // reset attempt counter on success
                    attempt = 0;

                    // Regular schedule: run once per day
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                    _logger.LogInformation("RefreshTokenCleanupJob is stopping due to cancellation");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;

                    // Log using job logger first
                    _logger.LogError(ex, "Error in RefreshTokenCleanupJob run (attempt {Attempt})", attempt);

                    // Also log using the same category as ExceptionHandlingMiddleware so logs look consistent
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
                        var bgLogger = loggerFactory?.CreateLogger("BankingSystemAPI.Presentation.Middlewares.ExceptionHandlingMiddleware");
                        if (bgLogger != null)
                        {
                            bgLogger.LogError(ex, ApiResponseMessages.Logging.MiddlewareExceptionHandled, new object[] { "Background", nameof(RefreshTokenCleanupJob), ex.GetType().Name, ex.Message });
                        }
                    }
                    catch
                    {
                        // ignore logging errors
                    }

                    if (attempt >= maxRetries)
                    {
                        _logger.LogCritical(ex, "RefreshTokenCleanupJob reached max retry attempts ({MaxRetries}). Stopping job.", maxRetries);
                        // Rethrow to allow host/global handlers to act (or stop the host)
                        throw;
                    }

                    // Exponential backoff with jitter, capped to 5 minutes
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 1000));
                    var backoffSeconds = Math.Pow(2, attempt); // 2,4,8...
                    var delay = TimeSpan.FromSeconds(Math.Min(backoffSeconds, 300)) + jitter;

                    _logger.LogInformation("Waiting {Delay} before next attempt for RefreshTokenCleanupJob", delay);

                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }
}

