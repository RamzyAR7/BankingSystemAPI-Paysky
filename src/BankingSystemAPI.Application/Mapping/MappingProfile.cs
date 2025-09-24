using AutoMapper;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Application.DTOs.Bank;

namespace BankingSystemAPI.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ConfigureUserMappings();
            ConfigureAccountMappings();
            ConfigureCurrencyMappings();
            ConfigureInterestLogMappings();
            ConfigureTransactionMappings();
            ConfigureBankMappings();
        }

        private void ConfigureUserMappings()
        {
            #region User
            CreateMap<ApplicationUser, UserResDto>()
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Accounts, opt => opt.MapFrom(src => src.Accounts))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.Bank != null ? src.Bank.Name : null));

            CreateMap<UserReqDto, ApplicationUser>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToDateTime(new TimeOnly(0, 0))))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username));

            CreateMap<UserEditDto, ApplicationUser>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToDateTime(new TimeOnly(0, 0))))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
                .ForMember(dest => dest.Accounts, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username));
            #endregion
        }

        private void ConfigureAccountMappings()
        {
            #region Account
            CreateMap<Account, AccountDto>()
                .Include<SavingsAccount, SavingsAccountDto>()
                .Include<CheckingAccount, CheckingAccountDto>()
                // map Currency.Code -> CurrencyCode in response
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Code : string.Empty))
                // ensure we map only scalar UserId and don't validate/traverse navigation properties
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForSourceMember(src => src.User, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.AccountTransactions, opt => opt.DoNotValidate());

            CreateMap<SavingsAccount, SavingsAccountDto>()
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => AccountType.Savings.ToString()))
                .ForMember(dest => dest.InterestType, opt => opt.MapFrom(src => src.InterestType.ToString()));

            CreateMap<CheckingAccount, CheckingAccountDto>()
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => AccountType.Checking.ToString()));

            CreateMap<SavingsAccountReqDto, SavingsAccount>()
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.InitialBalance))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AccountNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.InterestType, opt => opt.MapFrom(src => src.InterestType));

            CreateMap<CheckingAccountReqDto, CheckingAccount>()
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.InitialBalance))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AccountNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());
            #endregion
        }

        private void ConfigureCurrencyMappings()
        {
            #region Currency
            CreateMap<Currency, CurrencyDto>();
            CreateMap<CurrencyReqDto, Currency>();
            #endregion
        }

        private void ConfigureInterestLogMappings()
        {
            #region InterestLog
            CreateMap<InterestLog, InterestLogDto>();            
            #endregion
        }

        private void ConfigureTransactionMappings()
        {
            #region Transaction
            CreateMap<Transaction, TransactionResDto>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Id))
                // pick the AccountTransaction with Role == Source when available, otherwise fall back to first
                .ForMember(dest => dest.SourceAccountId, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Any(at => at.Role == TransactionRole.Source)
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Source).AccountId
                        : (src.AccountTransactions != null && src.AccountTransactions.Any()
                            ? src.AccountTransactions.First().AccountId
                            : (int?)null)))
                .ForMember(dest => dest.TargetAccountId, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Count > 1
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Target).AccountId
                        : (int?)null))
                .ForMember(dest => dest.SourceCurrency, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Any(at => at.Role == TransactionRole.Source)
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Source).TransactionCurrency
                        : (src.AccountTransactions != null && src.AccountTransactions.Any()
                            ? src.AccountTransactions.First().TransactionCurrency
                            : string.Empty)))
                .ForMember(dest => dest.TargetCurrency, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Count > 1
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Target).TransactionCurrency
                        : string.Empty))
                // convert enum to string representation
                .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.TransactionType.ToString()))
                // set TransactionRole to Source when available, otherwise fall back to first
                .ForMember(dest => dest.TransactionRole, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Any(at => at.Role == TransactionRole.Source)
                        ? TransactionRole.Source.ToString()
                        : (src.AccountTransactions != null && src.AccountTransactions.Any()
                            ? src.AccountTransactions.First().Role.ToString()
                            : string.Empty)))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src =>
                    // If there's a target account transaction (transfer), prefer its Amount so the DTO reflects what the target received
                    src.AccountTransactions != null && src.AccountTransactions.Count > 1
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Target).Amount
                        : (src.AccountTransactions != null && src.AccountTransactions.Any()
                            ? src.AccountTransactions.First().Amount
                            : 0m)))
                .ForMember(dest => dest.SourceAmount, opt => opt.MapFrom(src =>
                    // Map only the amount from the source account transaction (exclude fees)
                    src.AccountTransactions != null && src.AccountTransactions.Any()
                        ? (src.AccountTransactions.Any(at => at.Role == TransactionRole.Source)
                            ? src.AccountTransactions.First(at => at.Role == TransactionRole.Source).Amount
                            : src.AccountTransactions.First().Amount)
                        : 0m))
                .ForMember(dest => dest.TargetAmount, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Count > 1
                        ? src.AccountTransactions.First(at => at.Role == TransactionRole.Target).Amount
                        : (src.AccountTransactions != null && src.AccountTransactions.Any()
                            ? src.AccountTransactions.First().Amount
                            : 0m)))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.Fees, opt => opt.MapFrom(src =>
                    src.AccountTransactions != null && src.AccountTransactions.Any()
                        ? (src.AccountTransactions.Any(at => at.Role == TransactionRole.Source)
                            ? src.AccountTransactions.First(at => at.Role == TransactionRole.Source).Fees
                            : src.AccountTransactions.First().Fees)
                        : 0m));
            #endregion
        }

        private void ConfigureBankMappings()
        {
            CreateMap<Bank, BankResDto>()
                .ForMember(dest => dest.Users, opt => opt.Ignore());
            CreateMap<Bank, BankSimpleResDto>();
            CreateMap<BankReqDto, Bank>();
            CreateMap<BankEditDto, Bank>()
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()); // Prevent IsActive from being mapped in edit
        }
    }
}
