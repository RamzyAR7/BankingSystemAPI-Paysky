using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.TransactionSpecification;
using AutoMapper;
using System.Linq;
using BankingSystemAPI.Application.Interfaces.Authorization;

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
                await _accountAuth.CanViewAccountAsync(request.AccountId);
            }

            var spec = new TransactionsByAccountPagedSpecification(request.AccountId, (request.PageNumber - 1) * request.PageSize, request.PageSize);

            if (_transactionAuth is not null)
            {
                // use authorization service to filter transactions
                var query = _uow.TransactionRepository.QueryByAccountId(request.AccountId);
                var (items, total) = await _transactionAuth.FilterTransactionsAsync(query, request.PageNumber, request.PageSize);

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
