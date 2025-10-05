#region Usings
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Infrastructure.Configuration_Classes
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            builder.Property(t => t.TransactionType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(t => t.Timestamp)
                .IsRequired();
        }
    }
}

