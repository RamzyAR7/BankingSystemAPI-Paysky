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
    public class ApplicationDbContext:IdentityDbContext<ApplicationUser, ApplicationRole, string>
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
            builder.HasSequence<int>("AccountIdSequence");
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
        }
    }
}

