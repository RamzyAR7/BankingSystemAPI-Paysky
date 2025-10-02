using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus
{
    public class SetBankActiveStatusCommandHandler
            : ICommandHandler<SetBankActiveStatusCommand>
    {
        private readonly IUnitOfWork _uow;

        public SetBankActiveStatusCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Result> Handle(SetBankActiveStatusCommand request, CancellationToken cancellationToken)
        {
            var spec = new BankByIdSpecification(request.id);
            var bank = await _uow.BankRepository.FindAsync(spec);

            if (bank == null)
                return Result.Failure(new[] { $"Bank with ID '{request.id}' not found." });

            bank.IsActive = request.isActive;

            await _uow.BankRepository.UpdateAsync(bank);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}
