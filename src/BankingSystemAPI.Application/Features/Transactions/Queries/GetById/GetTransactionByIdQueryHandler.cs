using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using AutoMapper;
using BankingSystemAPI.Application.Interfaces.Authorization;
using System.Linq;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetById
{
    public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService? _transactionAuth;

        public GetTransactionByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ITransactionAuthorizationService? transactionAuth = null)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
        }

        public async Task<Result<TransactionResDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            if (_transactionAuth is not null)
            {
                var query = _uow.TransactionRepository.QueryWithAccountTransactions().Where(t => t.Id == request.Id);
                var (items, total) = await _transactionAuth.FilterTransactionsAsync(query, 1, 1);
                var trx = items.FirstOrDefault();
                if (trx == null) return Result<TransactionResDto>.Failure(new[] { "Transaction not found or access denied." });
                return Result<TransactionResDto>.Success(_mapper.Map<TransactionResDto>(trx));
            }

            var trxDefault = await _uow.TransactionRepository.GetByIdAsync(request.Id);
            if (trxDefault == null) return Result<TransactionResDto>.Failure(new[] { "Transaction not found." });
            return Result<TransactionResDto>.Success(_mapper.Map<TransactionResDto>(trxDefault));
        }
    }
}
