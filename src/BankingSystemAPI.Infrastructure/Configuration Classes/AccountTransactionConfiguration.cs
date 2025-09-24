using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Configuration_Classes
{
    public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
    {
        public void Configure(EntityTypeBuilder<AccountTransaction> builder)
        {
            builder.ToTable("AccountTransactions");

            builder.HasKey(at => new { at.AccountId, at.TransactionId });

            builder.Property(at => at.TransactionCurrency)
                .HasMaxLength(10);

            builder.Property(at => at.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(at => at.Fees)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            builder.Property(at => at.Role)
                .HasConversion<string>() 
                .IsRequired();

            builder.HasOne(at => at.Account)
                   .WithMany(a => a.AccountTransactions)
                   .HasForeignKey(at => at.AccountId);

            builder.HasOne(at => at.Transaction)
                   .WithMany(t => t.AccountTransactions)
                   .HasForeignKey(at => at.TransactionId);
        }
    }
}
