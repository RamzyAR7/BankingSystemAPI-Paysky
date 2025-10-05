#region Usings
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Linq;
#endregion


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
            Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobStartedFormat, nameof(AddInterestJob), DateTime.UtcNow));
            Console.ForegroundColor = originalColor;
            _logger.LogInformation(ApiResponseMessages.Infrastructure.JobStartedFormat, nameof(AddInterestJob), DateTime.UtcNow);

            const int batchSize = 100; // Tune as needed for your DB
            while (!stoppingToken.IsCancellationRequested)
            {
                var runTime = DateTime.UtcNow;
                var jobStart = DateTime.UtcNow;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobRunStartedFormat, nameof(AddInterestJob), runTime));
                Console.ForegroundColor = originalColor;
                _logger.LogInformation(ApiResponseMessages.Infrastructure.JobRunStartedFormat, nameof(AddInterestJob), runTime);

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
                                        _logger.LogInformation(ApiResponseMessages.Infrastructure.JobAppliedInterestFormat, nameof(AddInterestJob), interestAmount, accountBase.Id, accountBase.AccountNumber, DateTime.UtcNow);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobAppliedInterestFormat, nameof(AddInterestJob), interestAmount, accountBase.Id, accountBase.AccountNumber, DateTime.UtcNow));
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
                                    _logger.LogError(exAccount, ApiResponseMessages.Infrastructure.JobErrorProcessingAccountFormat, nameof(AddInterestJob), accountBase.Id, exAccount.Message);
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobErrorProcessingAccountFormat, nameof(AddInterestJob), accountBase.Id, exAccount.Message));
                                    Console.ForegroundColor = originalColor;
                                }
                            }
                            catch (Exception exAccount)
                            {
                                _logger.LogError(exAccount, ApiResponseMessages.Infrastructure.JobErrorProcessingAccountFormat, nameof(AddInterestJob), accountBase.Id, exAccount.Message);
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobErrorProcessingAccountFormat, nameof(AddInterestJob), accountBase.Id, exAccount.Message));
                                Console.ForegroundColor = originalColor;
                            }
                        }

                        // Commit after each page/batch
                        await uow.SaveAsync();
                        appliedCount += batchApplied;
                        var batchEnd = DateTime.UtcNow;
                        _logger.LogInformation(ApiResponseMessages.Logging.BatchProcessed, batchNumber, savingsAccounts.Count, batchApplied, (batchEnd - batchStart).TotalSeconds);

                        // Move to next page
                        pageNumber++;

                        // If we've processed all known records, stop
                        if ((pageNumber - 1) * batchSize >= total)
                        {
                            more = false;
                        }
                    }

                    var jobEnd = DateTime.UtcNow;
                    _logger.LogInformation(ApiResponseMessages.Infrastructure.JobRunCompletedFormat, nameof(AddInterestJob), totalAccounts, appliedCount, DateTime.UtcNow, (jobEnd - jobStart).TotalSeconds);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobRunCompletedFormat, nameof(AddInterestJob), totalAccounts, appliedCount, DateTime.UtcNow, (jobEnd - jobStart).TotalSeconds));
                    Console.ForegroundColor = originalColor;

                    // Run every 5 minutes for testing if any account uses every5minutes
                    // Note: we looked at the snapshot of accounts in this run; to decide delay accurately check DB again next run
                    var delay = TimeSpan.FromDays(1);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                    _logger.LogInformation(string.Format(ApiResponseMessages.Infrastructure.JobStoppingDueToCancellation, nameof(AddInterestJob)));
                    break;
                }
                catch (Exception exRun)
                {
                    _logger.LogError(exRun, ApiResponseMessages.Infrastructure.JobRunFailedFormat, nameof(AddInterestJob));
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format(ApiResponseMessages.Infrastructure.JobRunFailedFormat, nameof(AddInterestJob)) + $": {exRun.Message}");
                    Console.ForegroundColor = originalColor;

                    // Backoff a bit on error to avoid tight error loops
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}

