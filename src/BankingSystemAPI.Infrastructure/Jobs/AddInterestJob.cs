using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                    // Load all savings accounts with their interest logs
                    var accounts = await uow.AccountRepository.FindAllAsync(a => a is SavingsAccount, new[] { "InterestLogs" });
                    totalAccounts = accounts.Count();
                    var savingsAccounts = accounts.OfType<SavingsAccount>().ToList();

                    for (int i = 0; i < savingsAccounts.Count; i += batchSize)
                    {
                        var batch = savingsAccounts.Skip(i).Take(batchSize).ToList();
                        batchNumber++;
                        var batchStart = DateTime.UtcNow;
                        int batchApplied = 0;
                        foreach (var accountBase in batch)
                        {
                            try
                            {
                                if (ShouldAddInterest(accountBase))
                                {
                                    var amount = accountBase.Balance * accountBase.InterestRate / 100;
                                    accountBase.Balance += amount;
                                    var log = new InterestLog
                                    {
                                        Amount = amount,
                                        Timestamp = DateTime.UtcNow,
                                        SavingsAccountId = accountBase.Id,
                                        SavingsAccountNumber = accountBase.AccountNumber
                                    };
                                    await uow.InterestLogRepository.AddAsync(log);
                                    await uow.AccountRepository.UpdateAsync(accountBase);
                                    batchApplied++;
                                    _logger.LogInformation("Applied interest {Amount} to Savings Id={AccountId}, Number={AccountNumber}", amount, accountBase.Id, accountBase.AccountNumber);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"[AddInterestJob] Applied interest {amount:C} to Savings Id={accountBase.Id}, Number={accountBase.AccountNumber} at {DateTime.UtcNow:u}");
                                    Console.ForegroundColor = originalColor;
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
                                        if (ShouldAddInterest(accountBase))
                                        {
                                            var amount = accountBase.Balance * accountBase.InterestRate / 100;
                                            accountBase.Balance += amount;
                                            var log = new InterestLog
                                            {
                                                Amount = amount,
                                                Timestamp = DateTime.UtcNow,
                                                SavingsAccountId = accountBase.Id,
                                                SavingsAccountNumber = accountBase.AccountNumber
                                            };
                                            await uow.InterestLogRepository.AddAsync(log);
                                            await uow.AccountRepository.UpdateAsync(accountBase);
                                            batchApplied++;
                                            success = true;
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
                        // Commit after each batch
                        await uow.SaveAsync();
                        appliedCount += batchApplied;
                        var batchEnd = DateTime.UtcNow;
                        _logger.LogInformation("Batch {BatchNumber} processed {BatchCount} accounts, applied {BatchApplied} interest, duration {Duration}s", batchNumber, batch.Count, batchApplied, (batchEnd-batchStart).TotalSeconds);
                    }
                    var jobEnd = DateTime.UtcNow;
                    _logger.LogInformation("AddInterestJob run completed. TotalAccounts={Total}, Applied={Applied}, Duration={Duration}s", totalAccounts, appliedCount, (jobEnd-jobStart).TotalSeconds);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[AddInterestJob] run completed. TotalAccounts={totalAccounts}, Applied={appliedCount} at {DateTime.UtcNow:u}, Duration={(jobEnd-jobStart).TotalSeconds}s");
                    Console.ForegroundColor = originalColor;

                    // Run every 5 minutes for testing if any account uses every5minutes
                    bool hasFiveMinAccount = savingsAccounts.Any(a => a.InterestType == InterestType.every5minutes);
                    var delay = hasFiveMinAccount ? TimeSpan.FromMinutes(5) : TimeSpan.FromDays(1);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception exRun)
                {
                    _logger.LogError(exRun, "AddInterestJob run failed");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[AddInterestJob] run failed: {exRun.Message}");
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        private bool ShouldAddInterest(SavingsAccount account)
        {
            var lastLog = account.InterestLogs?
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            var referenceDate = lastLog?.Timestamp ?? account.CreatedDate;

            return account.InterestType switch
            {
                InterestType.Monthly =>
                    referenceDate.AddMonths(1) <= DateTime.UtcNow,

                InterestType.Quarterly =>
                    referenceDate.AddMonths(3) <= DateTime.UtcNow,

                InterestType.Annually =>
                    referenceDate.AddYears(1) <= DateTime.UtcNow,

                InterestType.every5minutes =>
                    referenceDate.AddMinutes(5) <= DateTime.UtcNow,

                _ => false
            };
        }

    }
}
