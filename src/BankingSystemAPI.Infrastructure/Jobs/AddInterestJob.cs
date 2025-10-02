using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Linq;

namespace BankingSystemAPI.Infrastructure.Jobs
{
    public class AddInterestJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AddInterestJob> _logger;

        public AddInterestJob(IServiceScopeFactory scopeFactory, ILogger<AddInterestJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[AddInterestJob] started at {DateTime.UtcNow:u}");
            Console.ForegroundColor = originalColor;
            _logger.LogInformation("AddInterestJob started at {StartTime}", DateTime.UtcNow);

            const int batchSize = 100; // Tune as needed for your DB
            while (!stoppingToken.IsCancellationRequested)
            {
                var runTime = DateTime.UtcNow;
                var jobStart = DateTime.UtcNow;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[AddInterestJob] run started at {runTime:u}");
                Console.ForegroundColor = originalColor;
                _logger.LogInformation("AddInterestJob run started at {RunTime}", runTime);

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                int totalAccounts = 0;
                int appliedCount = 0;
                int batchNumber = 0;
                try
                {
                    // Build base query for savings accounts including InterestLogs and Currency
                    var baseQuery = uow.AccountRepository.Table
                        .OfType<SavingsAccount>()
                        .Include(s => s.InterestLogs)
                        .Include(s => s.Currency)
                        .AsQueryable();

                    int pageNumber = 1;
                    bool more = true;

                    while (more && !stoppingToken.IsCancellationRequested)
                    {
                        var (accountsPage, total) = await uow.AccountRepository.GetFilteredAccountsAsync(baseQuery, pageNumber, batchSize);

                        if (pageNumber == 1)
                        {
                            totalAccounts = total;
                        }

                        var savingsAccounts = accountsPage.OfType<SavingsAccount>().ToList();
                        if (!savingsAccounts.Any())
                        {
                            more = false;
                            break;
                        }

                        batchNumber++;
                        var batchStart = DateTime.UtcNow;
                        int batchApplied = 0;

                        foreach (var accountBase in savingsAccounts)
                        {
                            try
                            {
                                if (accountBase.ShouldApplyInterest())
                                {
                                    var lastInterestDate = accountBase.GetLastInterestDate();
                                    var days = (DateTime.UtcNow - lastInterestDate).Days;
                                    var interestAmount = accountBase.CalculateInterest(days);
                                    if (interestAmount > 0)
                                    {
                                        accountBase.ApplyInterest(interestAmount, DateTime.UtcNow);
                                        await uow.AccountRepository.UpdateAsync(accountBase);
                                        batchApplied++;
                                        _logger.LogInformation("Applied interest {Amount} to Savings Id={AccountId}, Number={AccountNumber}", interestAmount, accountBase.Id, accountBase.AccountNumber);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"[AddInterestJob] Applied interest {interestAmount:C} to Savings Id={accountBase.Id}, Number={accountBase.AccountNumber} at {DateTime.UtcNow:u}");
                                        Console.ForegroundColor = originalColor;
                                    }
                                }
                            }
                            catch (DbUpdateConcurrencyException exAccount)
                            {
                                // Retry logic: try up to 3 times
                                int retries = 0;
                                bool success = false;
                                while (retries < 3 && !success)
                                {
                                    retries++;
                                    try
                                    {
                                        await Task.Delay(100 * retries, stoppingToken); // Exponential backoff
                                        if (accountBase.ShouldApplyInterest())
                                        {
                                            var lastInterestDate = accountBase.GetLastInterestDate();
                                            var days = (DateTime.UtcNow - lastInterestDate).Days;
                                            var interestAmount = accountBase.CalculateInterest(days);
                                            if (interestAmount > 0)
                                            {
                                                accountBase.ApplyInterest(interestAmount, DateTime.UtcNow);
                                                await uow.AccountRepository.UpdateAsync(accountBase);
                                                batchApplied++;
                                                success = true;
                                            }
                                        }
                                    }
                                    catch (DbUpdateConcurrencyException)
                                    {
                                        // continue retrying
                                    }
                                }
                                if (!success)
                                {
                                    _logger.LogError(exAccount, "Failed to process interest for account Id={AccountId} after retries", accountBase.Id);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"[AddInterestJob] Error processing account Id={accountBase.Id}: {exAccount.Message}");
                                    Console.ForegroundColor = originalColor;
                                }
                            }
                            catch (Exception exAccount)
                            {
                                _logger.LogError(exAccount, "Failed to process interest for account Id={AccountId}", accountBase.Id);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[AddInterestJob] Error processing account Id={accountBase.Id}: {exAccount.Message}");
                                Console.ForegroundColor = originalColor;
                            }
                        }

                        // Commit after each page/batch
                        await uow.SaveAsync();
                        appliedCount += batchApplied;
                        var batchEnd = DateTime.UtcNow;
                        _logger.LogInformation("Batch {BatchNumber} processed {BatchCount} accounts, applied {BatchApplied} interest, duration {Duration}s", batchNumber, savingsAccounts.Count, batchApplied, (batchEnd - batchStart).TotalSeconds);

                        // Move to next page
                        pageNumber++;

                        // If we've processed all known records, stop
                        if ((pageNumber - 1) * batchSize >= total)
                        {
                            more = false;
                        }
                    }

                    var jobEnd = DateTime.UtcNow;
                    _logger.LogInformation("AddInterestJob run completed. TotalAccounts={Total}, Applied={Applied}, Duration={Duration}s", totalAccounts, appliedCount, (jobEnd - jobStart).TotalSeconds);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[AddInterestJob] run completed. TotalAccounts={totalAccounts}, Applied={appliedCount} at {DateTime.UtcNow:u}, Duration={(jobEnd - jobStart).TotalSeconds}s");
                    Console.ForegroundColor = originalColor;

                    // Run every 5 minutes for testing if any account uses every5minutes
                    // Note: we looked at the snapshot of accounts in this run; to decide delay accurately check DB again next run
                    var delay = TimeSpan.FromDays(1);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                    _logger.LogInformation("AddInterestJob stopping due to cancellation.");
                    break;
                }
                catch (Exception exRun)
                {
                    _logger.LogError(exRun, "AddInterestJob run failed");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[AddInterestJob] run failed: {exRun.Message}");
                    Console.ForegroundColor = originalColor;

                    // Backoff a bit on error to avoid tight error loops
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}
