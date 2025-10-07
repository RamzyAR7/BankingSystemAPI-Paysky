#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById
{
    /// <summary>
    /// Simplified query handler for retrieving account by ID
    /// </summary>
    public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAccountAuthorizationService _accountAuth;

        public GetAccountByIdQueryHandler(IUnitOfWork uow, IMapper mapper, IAccountAuthorizationService accountAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _accountAuth = accountAuth;
        }

        public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            // Authorization
            var authResult = await _accountAuth.CanViewAccountAsync(request.Id);
            if (authResult.IsFailure)
                return Result<AccountDto>.Failure(authResult.ErrorItems);

            // Get account using specification
            var spec = new AccountByIdSpecification(request.Id);
            var account = await _uow.AccountRepository.FindAsync(spec);
            
            if (account == null)
                return Result<AccountDto>.NotFound("Account", request.Id);

            var dto = _mapper.Map<AccountDto>(account);
            return Result<AccountDto>.Success(dto);
        }
    }
}

