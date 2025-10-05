#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.BankSpecification;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.CreateBank
{
    public class CreateBankCommandHandler : ICommandHandler<CreateBankCommand, BankResDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateBankCommandHandler> _logger;

        public CreateBankCommandHandler(IUnitOfWork uow, IMapper mapper, ILogger<CreateBankCommandHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<BankResDto>> Handle(CreateBankCommand request, CancellationToken cancellationToken)
        {
            // Note: Input validation (null/empty checks) handled by CreateBankCommandValidator
            // This handler focuses on business logic validation and execution
            
            // Business validation: Check bank name uniqueness
            var uniquenessResult = await ValidateUniquenessAsync(request.bankDto);
            if (!uniquenessResult) // Using implicit bool operator!
                return Result<BankResDto>.Failure(uniquenessResult.Errors);

            var createResult = await CreateAndPersistBankAsync(request.bankDto);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
            {
                _logger.LogInformation("Bank created successfully: {BankName}", request.bankDto.Name);
            })
            .OnFailure(errors => 
            {
                _logger.LogWarning("Bank creation failed for {BankName}. Errors: {Errors}",
                    request.bankDto.Name, string.Join(", ", errors));
            });

            return createResult;
        }

        private async Task<Result> ValidateUniquenessAsync(BankReqDto dto)
        {
            var normalized = dto.Name.Trim();
            var normalizedLower = normalized.ToLowerInvariant();

            var spec = new BankByNormalizedNameSpecification(normalizedLower);
            var existing = await _uow.BankRepository.FindAsync(spec);
            
            return existing == null
                ? Result.Success()
                : Result.BadRequest("A bank with the same name already exists.");
        }

        private async Task<Result<BankResDto>> CreateAndPersistBankAsync(BankReqDto dto)
        {
            try
            {
                var entity = _mapper.Map<Bank>(dto);
                entity.Name = dto.Name.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsActive = true; // Set to true by default for new banks
                
                await _uow.BankRepository.AddAsync(entity);
                await _uow.SaveAsync();
                
                var result = _mapper.Map<BankResDto>(entity);
                return Result<BankResDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<BankResDto>.Failure(new[] { $"Failed to create bank: {ex.Message}" });
            }
        }
    }
}

