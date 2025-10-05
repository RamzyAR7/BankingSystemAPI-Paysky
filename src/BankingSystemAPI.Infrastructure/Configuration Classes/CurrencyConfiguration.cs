#region Usings
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Infrastructure.Configuration_Classes
{
    public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("Currencies");

            builder.Property(c => c.Code)
                .HasMaxLength(10)
                .IsRequired();

            builder.HasIndex(c => c.Code)
                .IsUnique();

            builder.Property(c => c.ExchangeRate)
                .HasColumnType("decimal(18,6)");

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true)
                .IsRequired();
        }
    }
}

