using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank
{
    public class DeleteBankCommandHandler : ICommandHandler<DeleteBankCommand, bool>
    {
        private readonly IUnitOfWork _uow;

        public DeleteBankCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<Result<bool>> Handle(DeleteBankCommand request, CancellationToken cancellationToken)
        {
            var spec = new BankByIdSpecification(request.id);
            var bank = await _uow.BankRepository.FindAsync(spec);

            if (bank == null)
                return Result<bool>.Failure(new[] { "Bank not found." });

            var hasUsers = await _uow.UserRepository.AnyAsync(u => u.BankId == request.id);
            if (hasUsers)
                return Result<bool>.Failure(new[] { "Cannot delete bank that has existing users." });

            await _uow.BankRepository.DeleteAsync(bank);
            await _uow.SaveAsync();

            return Result<bool>.Success(true);
        }
    }
}
