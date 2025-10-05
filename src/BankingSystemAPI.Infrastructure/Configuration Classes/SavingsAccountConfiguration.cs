#region Usings
using BankingSystemAPI.Domain.Constant;
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
    public class SavingsAccountConfiguration: IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            builder.UseTpcMappingStrategy();

            // Configure InterestRate with proper precision for banking calculations
            builder.Property(sa => sa.InterestRate)
                .HasColumnType("decimal(5,4)")
                .HasPrecision(5, 4)
                .IsRequired();

            // Configure InterestType enum to be stored as integer in database
            // This provides better performance and storage efficiency than string conversion
            builder.Property(sa => sa.InterestType)
                .HasConversion<int>()
                .HasColumnName("InterestType")
                .HasColumnType("int")
                .IsRequired()
                .HasComment("1=Monthly, 2=Quarterly, 3=Annually, 4=Every5Minutes(Testing)");

            // Configure relationship with InterestLogs
            builder.HasMany(sa => sa.InterestLogs)
                   .WithOne(il => il.SavingsAccount)
                   .HasForeignKey(il => il.SavingsAccountId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Add index on InterestType for better query performance
            builder.HasIndex(sa => sa.InterestType)
                   .HasDatabaseName("IX_SavingsAccount_InterestType");
        }
    }
}

