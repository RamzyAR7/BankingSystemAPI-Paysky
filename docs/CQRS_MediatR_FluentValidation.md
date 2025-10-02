# CQRS + MediatR + FluentValidation in BankingSystemAPI

## 1. Why CQRS
| Concern | Command Side | Query Side |
|---------|--------------|------------|
| Intent | Change state | Read state |
| Return | Result / Result<T> (no side?effect leakage) | Result<T> (projection) |
| Validation | Business + invariants | Input + existence |
| Performance | Transactional, may lock | Optimized read shapes |

Benefits:
- Separation of mutation vs read concerns
- Clearer dependency direction
- Easier performance tuning
- Enables pipeline behaviors (cross?cutting policies)

## 2. MediatR in This Solution
MediatR decouples controllers from application logic. Controllers construct *Command* or *Query* objects and send them through `IMediator`.

### 2.1 Definitions
```csharp
// Marker interfaces (conceptual)
public interface ICommand : IRequest<Result> {}
public interface ICommand<TResponse> : IRequest<Result<TResponse>> {}
public interface IQuery<TResponse> : IRequest<Result<TResponse>> {}

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand {}
public interface ICommandHandler<TCommand,TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> {}
public interface IQueryHandler<TQuery,TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> {}
```

### 2.2 Example Command
```csharp
public sealed record WithdrawCommand(WithdrawReqDto Req) : ICommand<TransactionResDto>;
```

### 2.3 Example Handler (Simplified)
```csharp
public class WithdrawCommandHandler : ICommandHandler<WithdrawCommand, TransactionResDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public async Task<Result<TransactionResDto>> Handle(WithdrawCommand request, CancellationToken ct)
    {
        if (request.Req.Amount <= 0)
            return Result<TransactionResDto>.BadRequest("Amount must be > 0.");

        var acc = await _uow.AccountRepository.GetByIdAsync(request.Req.AccountId);
        if (acc is null)
            return Result<TransactionResDto>.NotFound("Account", request.Req.AccountId);

        if (request.Req.Amount > acc.GetAvailableBalance())
            return Result<TransactionResDto>.BadRequest("Insufficient funds.");

        acc.Withdraw(request.Req.Amount);
        await _uow.AccountRepository.UpdateAsync(acc);
        await _uow.SaveAsync();

        var dto = _mapper.Map<TransactionResDto>(CreateTransaction(acc, request.Req.Amount));
        return Result<TransactionResDto>.Success(dto);
    }
}
```

### 2.4 Example Query
```csharp
public sealed record GetAccountByIdQuery(int Id) : IQuery<AccountDto>;

public class GetAccountByIdHandler : IQueryHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IUnitOfWork _uow; private readonly IMapper _mapper;
    public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery q, CancellationToken ct)
    {
        var entity = await _uow.AccountRepository.GetByIdAsync(q.Id);
        if (entity == null) return Result<AccountDto>.NotFound("Account", q.Id);
        return Result<AccountDto>.Success(_mapper.Map<AccountDto>(entity));
    }
}
```

## 3. FluentValidation Integration
All requests entering the pipeline pass through a MediatR `ValidationBehavior` that discovers registered validators for the specific request type.

### 3.1 Validator Example
```csharp
public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.Req).NotNull();
        When(x => x.Req != null, () =>
        {
            RuleFor(x => x.Req.AccountId).GreaterThan(0);
            RuleFor(x => x.Req.Amount).GreaterThan(0m);
        });
    }
}
```

### 3.2 Pipeline Behavior (Conceptual Flow)
```
Controller -> Mediator -> ValidationBehavior
  If invalid => Result.Failure(errors)
  Else => Next() -> Handler
```

### 3.3 Unified Failure Return
`ValidationBehavior` inspects `TResponse`; if it is `Result` or `Result<T>` it creates a failed result instead of throwing, minimizing exceptions.

## 4. End-to-End Command Flow (Sequence)
```
HTTP POST /api/accounts/withdraw
  -> Controller constructs WithdrawCommand
    -> mediator.Send(command)
       -> ValidationBehavior (FluentValidation rules)
          -> Handler (domain rules + persistence)
             -> Returns Result<TransactionResDto>
    -> Controller.HandleResult(Result)
       -> 200 OK or 400 with uniform error envelope
```

## 5. Why This Stack
| Feature | Benefit |
|---------|---------|
| CQRS | Clear separation of reads & writes |
| MediatR | Decouples controllers from handlers; testable |
| FluentValidation | Declarative, reusable rule definitions |
| Result Pattern | Consistent error contract; no business exceptions |
| Pipeline Behaviors | Centralized cross-cutting (validation, future logging, caching) |

## 6. Testing Guidance
| Level | Strategy |
|-------|----------|
| Validator | Directly instantiate validator; assert failures/success |
| Handler | Inject fakes / in-memory db; assert `Result<T>` semantics |
| Controller | Minimal since logic is in handlers; focus on routing + status codes |
| Integration | Simulate full request to ensure pipeline wiring |

## 7. Extension Ideas
| Idea | Description |
|------|-------------|
| CachingBehavior | Short-circuit queries with memory/redis caching |
| LoggingBehavior | Log timing + success/failure counts |
| AuthorizationBehavior | Centralize role/claim checks before handler |
| IdempotencyBehavior | Prevent duplicate command processing |

## 8. Anti-Patterns Avoided
| Anti-Pattern | Avoidance Mechanism |
|-------------|--------------------|
| Fat Controllers | Handlers + pipeline |
| Silent Exception Swallowing | Explicit Result return |
| Mixed Return + Throw | Pure Result discipline |
| Validation Scattering | FluentValidation centralization |

---
**Summary:** CQRS + MediatR + FluentValidation + Result pattern yields a predictable, testable, extensible application core with minimal ceremony and high cohesion.
