# Result Pattern Architecture & Flow Documentation

## 1. Overview
This document describes the Result pattern implementation across the BankingSystemAPI solution (.NET 8, C# 12). It unifies error handling, validation, authorization, and response formatting using:
- Result / Result<T>
- ValidationBehavior (MediatR Pipeline)
- Consistent controller handling via BaseApiController
- Infrastructure-only exception middleware
- Authorization helpers (BankGuard)
- ValidationService
- Functional extension helpers (ResultExtensions)

## 2. Goals
| Goal | Achieved By |
|------|-------------|
| Eliminate business exceptions | Return Result / Result<T> |
| Consistent API responses | BaseApiController helpers |
| Distinguish infra vs business failures | ExceptionHandlingMiddleware |
| Composable validation & auth | Result.Combine / extensions |
| Testable domain logic | Pure Result return values |
| Reduced overhead | Avoid exception throwing for business rules |
| Predictable contracts | Uniform JSON envelopes |
| Easier monitoring | Structured error payload (ErrorDetails) |

## 3. Core Building Blocks
### 3.1 Result / Result<T>
Location: `src/BankingSystemAPI.Domain/Common/Result.cs`
Key features:
- `IsSuccess`, `IsFailure`, `Errors`, `ErrorMessage`
- Convenience factory methods: `NotFound`, `BadRequest`, `Forbidden`, `Unauthorized`
- Generic value container: `Result<T>.Success(value)`
- Aggregation: `Result.Combine(params Result[])`
- Mapping / transformation: `Map`, `Bind`, `MapAsync`, `BindAsync`
- Immutable outward API

### 3.2 Controller Base Handling
Location: `BaseApiController`
- Response shaping centralization
- No leaking of internal exception messages for business rules
- Uniform negative response body (400s)

### 3.3 Exception Middleware (Infrastructure Only)
Location: `ExceptionHandlingMiddleware`
- Handles: DB, concurrency, timeout, argument, unauthorized, unknown
- Does NOT handle business rule violations (already expressed as Results)
- Adds: `RequestId`, `Timestamp`, safe message, code

### 3.4 ValidationBehavior (MediatR)
Location: `ValidationBehavior.cs`
- Fast?fail on FluentValidation errors
- Returns typed failed Results (no exception cost) when handlers are Result-based

### 3.5 ValidationService
Centralizes domain invariants & cross?entity checks:
- Account existence / activity
- User / bank / currency state
- Transfer preconditions (ownership, funds, active status)
- Business rule branch isolation

### 3.6 Authorization Helper (BankGuard)
- Pure Result semantics
- Legacy throwing method retained with `[Obsolete]` for phased removal

### 3.7 Result Extensions
Adds composability & functional pipeline style:
- `Bind` / `BindAsync` (monadic chaining)
- `MapAsync`
- `OnSuccess`, `OnFailure`
- `ValidateAll` / `Result.Combine`

## 4. Detailed Lifecycle (Happy Path & Failure)
```mermaid
graph TD
A[HTTP Request] --> B[Controller]
B --> C[MediatR Send]
C --> D[ValidationBehavior]
D -->|Valid| E[Handler]
D -->|Invalid| R[Result<T>.Failure(errors)]
E --> F[Domain / ValidationService]
F -->|Fail| R
F -->|Success| G[Compose DTO]
G --> H[Result<T>.Success]
R --> I[Controller.HandleResult]
H --> I[Controller.HandleResult]
I --> J[(2xx / 400 JSON)]
E -->|Infrastructure exception| M[Bubble Exception]
M --> N[Exception Middleware]
N --> O[(409 / 500 ErrorDetails JSON)]
```

## 5. Request ? Response Flow (Expanded)
| Stage | Responsibility | Failure Expression |
|-------|----------------|--------------------|
| Controller | Compose command/query | N/A (business stays in handlers) |
| ValidationBehavior | FluentValidation | Failed Result or ValidationException (non-Result handlers) |
| Handler | Orchestrate domain logic | Result.Failure / NotFound / Forbidden |
| ValidationService | Entity & rule checks | Result<T>.Failure variants |
| Authorization (BankGuard) | Isolation & access | Result.Forbidden / Unauthorized |
| Persistence | Save / concurrency | Throws (caught by middleware) |
| Middleware | Map infra exceptions | ErrorDetails payload |

## 6. Error Taxonomy
| Category | Origin | Representation | HTTP Code | Example |
|----------|--------|----------------|-----------|---------|
| Validation | FluentValidation | `Result.Failure(errors)` | 400 | "Amount must be > 0" |
| Business Rule | Handler / ValidationService | `Result.BadRequest(...)` | 400 | "Insufficient funds." |
| Authorization | BankGuard / Services | `Result.Forbidden(...)` | 400* (optionally 403 if desired) | "Access forbidden due to bank isolation policy." |
| Not Found | ValidationService | `Result<T>.NotFound(...)` | 400* (optionally 404 mapping layer) | "Account with ID '10' not found." |
| Infrastructure | EF / System | Exception + Middleware | 400/409/500 | DB concurrency |
| Security (system) | UnauthorizedAccessException | Middleware | 401 | Unauthorized |

> NOTE: Currently business failures map to 400. You can introduce mapping logic to translate `NotFound` & `Forbidden` to 404 / 403 if required later.

## 7. Response Contracts
### 7.1 Success (value)
```json
{
  "id": 42,
  "accountNumber": "ACC00042",
  "balance": 1500.75,
  "currency": "USD"
}
```
### 7.2 Business Failure
```json
{
  "success": false,
  "errors": ["Insufficient funds."],
  "message": "Insufficient funds."
}
```
### 7.3 Infrastructure Failure (Middleware)
```json
{
  "code": "409",
  "message": "A concurrency conflict occurred. Please refresh and try again.
",
  "requestId": "04a2f5e4-a6dd-4e0d-9a95-5f7e7b0d3a5c",
  "timestamp": "2025-01-14T12:34:56.789Z"
}
```
### 7.4 Validation (FluentValidation Path)
If flowing through Result pipeline directly ? Business Failure structure.
If a non-Result handler: middleware returns `ErrorDetails` containing structured `Details` map.

## 8. Concurrency Handling Strategy
| Aspect | Approach |
|--------|----------|
| Detection | EF Core row version / concurrency tokens (implicit) |
| Retry | Local retry (e.g., withdraw) with back?off & entity reload |
| User feedback | 409 Conflict + safe message |
| Business rule unaffected | Concurrency never expressed as business failure inside handler |

## 9. Security & Isolation
| Concern | Mitigation |
|---------|-----------|
| Bank isolation | `BankGuard.ValidateSameBank` / `ValidateBankAccess` |
| Unauthorized access | Standard ASP.NET Core auth + optional `Result.Unauthorized()` from domain routines |
| Token mismatch / session invalid | Middleware or auth handlers (outside Result pattern) |
| Error leakage | Middleware emits generic infra messages |

## 10. Testing Matrix
| Test Type | Old Style | New Style |
|-----------|-----------|-----------|
| Business rule fail | Catch exception | Assert `result.IsFailure` & `Errors` |
| Validation fail | Exception path + varied | Assert failed Result in handler (or property bag if non-Result) |
| Success mapping | Check raw object | Assert `result.Value` not null & shaped properly |
| Concurrency | Forced retry & catch | Force concurrency ? expect 409 via middleware (integration) |
| Authorization | Expect thrown Forbidden | Assert `Result.Forbidden` and 400 response |

## 11. Performance Considerations
| Dimension | Exception Path (Old) | Result Path (New) |
|-----------|----------------------|-------------------|
| Allocation | High (stack trace) | Minimal (array/list of strings) |
| Branch cost | Unpredictable | Predictable O(1) |
| GC pressure | Elevated | Lower |
| Throughput (rule heavy endpoints) | Degraded | Stable |

## 12. Extensibility Patterns
| Need | Recommendation |
|------|----------------|
| Add pagination wrapper | Introduce `PagedResult<T>` inheriting from `Result` or separate composition |
| Domain events | Emit events after `Result.Success` in handler (pre-controller) |
| Telemetry | Add pipeline behavior wrapping handlers, logging `IsSuccess` & `Errors.Count` |
| Internationalization | Post?process `Errors` list with translation service in controller or filter |
| HTTP code mapping | Add a `ResultMetadata` object or discriminated union style type |

## 13. Troubleshooting Guide
| Symptom | Cause | Fix |
|---------|-------|-----|
| Always 400 for not found | No layer mapping to 404 | Add translator (e.g., filter) that inspects error text/pattern |
| Empty `Errors` but failure | Incorrect custom factory usage | Ensure `Failure(params string[])` always passed non-empty array |
| Concurrency floods logs | Hot row contention | Introduce jitter/backoff or queue writes |
| Mixed exception + Result in handler | Legacy code left | Refactor to return `Result` consistently |

## 14. FAQ
| Question | Answer |
|----------|--------|
| Why not exceptions for business rules? | Performance + clarity + composability |
| Can we map forbidden to 403 later? | Yes—add mapping layer in BaseApiController or custom filter |
| How to propagate correlation IDs? | Extend middleware to read `X-Correlation-ID` and set `RequestId` |
| Does this block throwing entirely? | Only for business logic; infra/system still throws |
| Can we log every failure? | Add pipeline behavior or `OnFailure` extension usage |

## 15. Migration Recap
| Phase | Completed |
|-------|-----------|
| Foundation (Result, Base Controller) | ? |
| Middleware simplification | ? |
| Authorization conversion | ? |
| Validation service rewrite | ? |
| Controller unification | ? |
| Handler adjustments | ? |
| Test alignment | ? |

## 16. Minimal Diff Example (Before ? After)
**Before:**
```csharp
if (account == null) throw new NotFoundException("Account not found");
if (amount <= 0) throw new BusinessException("Invalid amount");
```
**After:**
```csharp
if (account == null) return Result<Account>.NotFound("Account", id);
if (amount <= 0) return Result<Account>.BadRequest("Amount must be greater than zero.");
```

## 17. Recommended Future Enhancements
| Priority | Enhancement | Benefit |
|----------|-------------|---------|
| High | HTTP status mapping for semantic errors | Better REST alignment |
| Medium | Telemetry (success/failure counters) | Observability |
| Medium | Distributed tracing integration (OpenTelemetry) | Cross-service debugging |
| Low | Result code catalog / enum | Machine-friendly error taxonomy |
| Low | Custom problem+json RFC7807 formatter | Standards-based responses |

## 18. Reference Files
| Purpose | Path |
|---------|------|
| Result core | Domain/Common/Result.cs |
| Base controller | Presentation/Controllers/BaseApiController.cs |
| Middleware | Presentation/Middlewares/ExceptionHandlingMiddleware.cs |
| Error contract | Presentation/Helpers/ErrorDetails.cs |
| Extensions | Application/Extensions/ResultExtensions.cs |
| ValidationService | Application/Services/ValidationService.cs |
| Guard | Application/Authorization/Helpers/BankGuard.cs |
| Behavior | Application/Behaviors/ValidationBehavior.cs |
| Architecture Doc | docs/ResultPatternArchitecture.md |

## 19. Appendix: ErrorDetails Contract
```csharp
public class ErrorDetails {
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public IDictionary<string, string[]?>? Details { get; set; }
}
```
**Usage:** Emitted only by middleware for infrastructural / pipeline failures.

## 20. Summary
The Result pattern centralizes and clarifies business failure semantics, isolates infrastructure concerns, reduces overhead, and produces a predictable contract. This enables easier evolution (metrics, mapping, i18n) without disruptive refactors.

---
*End of extended architecture documentation.*
