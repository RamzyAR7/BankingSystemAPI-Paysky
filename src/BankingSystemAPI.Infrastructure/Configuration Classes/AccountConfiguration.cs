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
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.Property(a => a.Id)
                    .UseSequence("AccountIdSequence");

            builder.Property(a => a.AccountNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(a => a.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Property(a => a.CreatedDate)
                .IsRequired();

            builder.Property(a => a.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

            builder.Property(a => a.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasOne(a => a.Currency)
                   .WithMany(c => c.Accounts)
                   .HasForeignKey(a => a.CurrencyId);

            builder.UseTpcMappingStrategy();
        }
    }
}
