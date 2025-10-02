using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.TransactionSpecification;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId
{
    public class GetTransactionsByAccountQueryHandler : IQueryHandler<GetTransactionsByAccountQuery, IEnumerable<TransactionResDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService? _transactionAuth;
        private readonly IAccountAuthorizationService? _accountAuth;

        public GetTransactionsByAccountQueryHandler(IUnitOfWork uow, IMapper mapper, ITransactionAuthorizationService? transactionAuth = null, IAccountAuthorizationService? accountAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
            _accountAuth = accountAuth;
        }

        public async Task<Result<IEnumerable<TransactionResDto>>> Handle(GetTransactionsByAccountQuery request, CancellationToken cancellationToken)
        {
            // Ensure caller can view the account first
            if (_accountAuth is not null)
            {
                var authResult = await _accountAuth.CanViewAccountAsync(request.AccountId);
                if (authResult.IsFailure)
                    return Result<IEnumerable<TransactionResDto>>.Failure(authResult.Errors);
            }

            var spec = new TransactionsByAccountPagedSpecification(request.AccountId, (request.PageNumber - 1) * request.PageSize, request.PageSize);

            if (_transactionAuth is not null)
            {
                // use authorization service to filter transactions
                var query = _uow.TransactionRepository.QueryByAccountId(request.AccountId);
                var filterResult = await _transactionAuth.FilterTransactionsAsync(query, request.PageNumber, request.PageSize);

                if (filterResult.IsFailure)
                    return Result<IEnumerable<TransactionResDto>>.Failure(filterResult.Errors);

                var (items, total) = filterResult.Value!;

                // Extra safety: ensure transactions reference the requested account id
                var filtered = items.Where(t => t.AccountTransactions != null && t.AccountTransactions.Any(at => at.AccountId == request.AccountId)).ToList();

                var dtoItems = filtered.Select(i => _mapper.Map<TransactionResDto>(i)).ToList();
                return Result<IEnumerable<TransactionResDto>>.Success(dtoItems);
            }

            var (itemsDefault, totalDefault) = await _uow.TransactionRepository.GetPagedAsync(spec);
            var dtoItemsDefault = itemsDefault.Select(i => _mapper.Map<TransactionResDto>(i)).ToList();
            return Result<IEnumerable<TransactionResDto>>.Success(dtoItemsDefault);
        }
    }
}
