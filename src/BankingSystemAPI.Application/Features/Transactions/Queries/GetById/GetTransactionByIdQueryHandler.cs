using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.TransactionSpecification;
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
                var filterResult = await _transactionAuth.FilterTransactionsAsync(query, 1, 1);
                
                if (filterResult.IsFailure)
                    return Result<TransactionResDto>.Failure(filterResult.Errors);

                var (items, total) = filterResult.Value!;
                var trx = items.FirstOrDefault();
                if (trx == null) 
                    return Result<TransactionResDto>.NotFound("Transaction", request.Id);
                
                return Result<TransactionResDto>.Success(_mapper.Map<TransactionResDto>(trx));
            }

            var trxDefault = await _uow.TransactionRepository.GetByIdAsync(request.Id);
            if (trxDefault == null) 
                return Result<TransactionResDto>.NotFound("Transaction", request.Id);
            
            return Result<TransactionResDto>.Success(_mapper.Map<TransactionResDto>(trxDefault));
        }
    }
}
