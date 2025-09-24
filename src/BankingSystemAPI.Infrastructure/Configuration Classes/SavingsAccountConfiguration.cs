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
    public class SavingsAccountConfiguration: IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            builder.UseTpcMappingStrategy();

            builder.Property(sa => sa.InterestRate)
                .HasColumnType("decimal(5,2)");

            builder.Property(sa => sa.InterestType)
                .HasConversion<string>();

            builder.HasMany(sa => sa.InterestLogs)
                   .WithOne(il => il.SavingsAccount)
                   .HasForeignKey(il => il.SavingsAccountId);
        }
    }
}
