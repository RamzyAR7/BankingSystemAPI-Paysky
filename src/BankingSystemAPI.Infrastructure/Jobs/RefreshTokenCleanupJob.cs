using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cyan = "\u001b[36m";
            var reset = "\u001b[0m";

            var scanMsg = $"{cyan}[CleanupJob] Scanning for expired/inactive refresh tokens...{reset}";
            Console.WriteLine(scanMsg);

            const int batchSize = 100; // Tune as needed for your DB
            int totalTokens = 0;
            int removedTokens = 0;
            int batchNumber = 0;
            var jobStart = DateTime.UtcNow;

            // Query only expired/inactive tokens from DB
            var expiredTokensQuery = context.Set<RefreshToken>()
                .Where(rt => rt.IsExpired || rt.IsAbsoluteExpired || rt.RevokedOn != null);
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
                                await Task.Delay(100 * retries, cancellationToken); // Exponential backoff
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
                await context.SaveChangesAsync(cancellationToken); // Commit after each batch
                removedTokens += batchRemoved;
                var batchEnd = DateTime.UtcNow;
                _logger.LogInformation("CleanupJob Batch {BatchNumber} removed {BatchRemoved} tokens, duration {Duration}s", batchNumber, batchRemoved, (batchEnd-batchStart).TotalSeconds);
            }
            var jobEnd = DateTime.UtcNow;
            var foundMsg = $"{cyan}[CleanupJob] Found {totalTokens} tokens to clean.{reset}";
            Console.WriteLine(foundMsg);
            var removedMsg = $"{cyan}[CleanupJob] Removed {removedTokens} expired/invalid refresh tokens.{reset}";
            Console.WriteLine(removedMsg);
            _logger.LogInformation("CleanupJob run completed. TotalTokens={Total}, Removed={Removed}, Duration={Duration}s", totalTokens, removedTokens, (jobEnd-jobStart).TotalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cyan = "\u001b[36m";
            var reset = "\u001b[0m";
            var startMsg = $"{cyan}[CleanupJob] started at {DateTime.UtcNow:u}{reset}";
            Console.WriteLine(startMsg);
            // Run every 24 hours
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"{cyan}[CleanupJob] Error while cleaning refresh tokens{reset}";
                    Console.WriteLine(errorMsg);
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
