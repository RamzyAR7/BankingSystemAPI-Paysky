using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetById
{
    public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, TransactionResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService _transactionAuth;

        public GetTransactionByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ITransactionAuthorizationService transactionAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
        }

        public async Task<Result<TransactionResDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            var query = _uow.TransactionRepository.QueryWithAccountTransactions().Where(t => t.Id == request.Id);
            var filterResult = await _transactionAuth.FilterTransactionsAsync(query, 1, 1);

            if (filterResult.IsFailure)
                return Result<TransactionResDto>.Failure(filterResult.ErrorItems);

            var (items, total) = filterResult.Value!;
            var trx = items.FirstOrDefault();
            if (trx == null)
                return Result<TransactionResDto>.NotFound(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, "Transaction", request.Id));

            return Result<TransactionResDto>.Success(_mapper.Map<TransactionResDto>(trx));
        }
    }
}
