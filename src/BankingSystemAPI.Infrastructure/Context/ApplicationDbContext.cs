#region Usings
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Configuration_Classes;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
#endregion


namespace BankingSystemAPI.Infrastructure.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public DbSet<Bank> Banks { get; set; }
        public DbSet<CheckingAccount> CheckingAccounts { get; set; }
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<AccountTransaction> AccountTransactions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<InterestLog> InterestLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            // Define a single sequence that all Account tables will share for their primary key.
            // This ensures that Account IDs are globally unique across all account types.
            // Some providers (SQLite) do not support sequences. Skip sequence creation for those providers.
            try
            {
                var provider = this.Database.ProviderName ?? string.Empty;
                if (!provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    builder.HasSequence<int>("AccountIdSequence");
                }
            }
            catch
            {
                // If Database is not yet configured or provider cannot be determined, fallback to creating the sequence.
                // In rare cases this may throw for unsupported providers; callers that need SQLite should ensure
                // the provider is set before calling EnsureCreated.
                try { builder.HasSequence<int>("AccountIdSequence"); } catch { }
            }
            builder.ApplyConfiguration(new BankConfiguration());
            builder.ApplyConfiguration(new ApplicationUserConfiguration());
            builder.ApplyConfiguration(new ApplicationRoleConfiguration());
            builder.ApplyConfiguration(new RefreshTokenConfiguration());
            builder.ApplyConfiguration(new AccountConfiguration());
            builder.ApplyConfiguration(new CheckingAccountConfiguration());
            builder.ApplyConfiguration(new SavingsAccountConfiguration());
            builder.ApplyConfiguration(new InterestLogConfiguration());
            builder.ApplyConfiguration(new TransactionConfiguration());
            builder.ApplyConfiguration(new AccountTransactionConfiguration());
            builder.ApplyConfiguration(new CurrencyConfiguration());

            // Provider-specific adjustments: SQLite does not support SQL Server's GETUTCDATE().
            // Replace any defaultValueSql using GETUTCDATE() with CURRENT_TIMESTAMP for SQLite.
            try
            {
                var provider = this.Database.ProviderName ?? string.Empty;
                if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var entityType in builder.Model.GetEntityTypes())
                    {
                        foreach (var prop in entityType.GetProperties())
                        {
                            var defSql = prop.GetDefaultValueSql();
                            if (!string.IsNullOrEmpty(defSql) && defSql.Contains("GETUTCDATE", StringComparison.OrdinalIgnoreCase))
                            {
                                prop.SetDefaultValueSql("CURRENT_TIMESTAMP");
                            }
                            // If RowVersion is configured to be database-generated, SQLite cannot generate it.
                            // Make EF send the value by disabling ValueGenerated for SQLite when the property is a byte[] named RowVersion.
                            if (string.Equals(prop.Name, "RowVersion", StringComparison.OrdinalIgnoreCase)
                                && prop.ClrType == typeof(byte[]))
                            {
                                prop.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If anything goes wrong (e.g., Database not configured), ignore and proceed.
            }
        }
    }
}

