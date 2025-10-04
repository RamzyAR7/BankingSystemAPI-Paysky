using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Base controller providing consistent Result pattern handling with proper HTTP status codes
    /// Optimized to work seamlessly with semantic Result factory methods
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        private ILogger? _logger;
        
        protected ILogger Logger => _logger ??= HttpContext.RequestServices.GetService<ILogger<BaseApiController>>()!;

        /// <summary>
        /// Handle Result<T> responses with proper HTTP status codes based on error type
        /// Enhanced to work with semantic Result factory methods
        /// </summary>
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();
                return Ok(result.Value);
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result<T> for update scenarios (returns success message instead of data)
        /// Use this for PUT operations where you want a success message rather than returning the updated object
        /// </summary>
        protected IActionResult HandleUpdateResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();
                return Ok(new { success = true, message = GetSuccessMessage() });
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result (non-generic) responses with proper HTTP status codes and specific success messages
        /// Enhanced to work with semantic Result factory methods
        /// </summary>
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();
                return Ok(new { success = true, message = GetSuccessMessage() });
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result<T> for creation scenarios (returns 201 Created)
        /// </summary>
        protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName = "", object? routeValues = null)
        {
            if (result.IsSuccess)
            {
                Logger.LogInformation("Resource created successfully. Controller: {Controller}, Action: {Action}", 
                    GetControllerName(), GetActionName());

                return string.IsNullOrEmpty(actionName)
                    ? CreatedAtAction(null, null, result.Value)
                    : CreatedAtAction(actionName, routeValues, result.Value);
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Get success message based on HTTP method and controller context
        /// </summary>
        private string GetSuccessMessage()
        {
            var httpMethod = HttpContext.Request.Method;
            var controllerName = GetControllerName();
            var actionName = GetActionName();

            return httpMethod switch
            {
                "DELETE" => GetDeleteMessage(controllerName, actionName),
                "PUT" => GetUpdateMessage(controllerName, actionName),
                "PATCH" => GetUpdateMessage(controllerName, actionName),
                "POST" => GetProcessMessage(controllerName, actionName),
                _ => ApiResponseMessages.Generic.OperationCompleted
            };
        }

        /// <summary>
        /// Get delete success message
        /// </summary>
        private static string GetDeleteMessage(string controllerName, string actionName)
        {
            // Check for special delete actions
            if (actionName.Contains("deactivate", StringComparison.OrdinalIgnoreCase))
            {
                return GetDeactivateMessage(controllerName);
            }

            // Standard delete messages by controller
            return controllerName switch
            {
                "user" or "users" => ApiResponseMessages.Delete.User.Success,
                "bank" or "banks" => ApiResponseMessages.Delete.Bank.Success,
                "role" or "roles" => ApiResponseMessages.Delete.Role.Success,
                "account" or "accounts" => ApiResponseMessages.Delete.Account.Success,
                "currency" or "currencies" => ApiResponseMessages.Delete.Currency.Success,
                "checkingaccount" => ApiResponseMessages.Delete.CheckingAccount.Success,
                "savingsaccount" => ApiResponseMessages.Delete.SavingsAccount.Success,
                _ => ApiResponseMessages.Delete.Generic.Success
            };
        }

        /// <summary>
        /// Get update success message
        /// Enhanced to provide specific messages for different update scenarios
        /// </summary>
        private string GetUpdateMessage(string controllerName, string actionName)
        {
            // Check for password updates
            if (actionName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("changepassword", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Update.Password.Success;
            }

            // Check for status updates (active/inactive)
            if (actionName.Contains("active", StringComparison.OrdinalIgnoreCase) || 
                actionName.Contains("status", StringComparison.OrdinalIgnoreCase))
            {
                return GetStatusMessage(controllerName);
            }

            // Check for specific update operations based on action name
            var specificMessage = GetSpecificUpdateMessage(controllerName, actionName);
            if (!string.IsNullOrEmpty(specificMessage))
            {
                return specificMessage;
            }

            // Standard update messages by controller
            return controllerName switch
            {
                "user" or "users" => ApiResponseMessages.Update.User.Success,
                "bank" or "banks" => ApiResponseMessages.Update.Bank.Success,
                "role" or "roles" => ApiResponseMessages.Update.Role.Success,
                "account" or "accounts" => ApiResponseMessages.Update.Account.Success,
                "currency" or "currencies" => ApiResponseMessages.Update.Currency.Success,
                "checkingaccount" => ApiResponseMessages.Update.CheckingAccount.Success,
                "savingsaccount" => ApiResponseMessages.Update.SavingsAccount.Success,
                _ => ApiResponseMessages.Update.Generic.Success
            };
        }

        /// <summary>
        /// Get specific update message based on action patterns
        /// </summary>
        private static string GetSpecificUpdateMessage(string controllerName, string actionName)
        {
            // Profile/Personal information updates
            if (actionName.Contains("profile", StringComparison.OrdinalIgnoreCase))
            {
                return controllerName switch
                {
                    "user" or "users" => ApiResponseMessages.Update.User.ProfileSuccess,
                    _ => ApiResponseMessages.Update.Generic.ProfileSuccess
                };
            }

            // Contact information updates  
            if (actionName.Contains("contact", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Update.User.ContactSuccess;
            }

            // Security settings updates
            if (actionName.Contains("security", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Update.Generic.SecuritySuccess;
            }

            // Role assignment updates
            if (actionName.Contains("role", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("assign", StringComparison.OrdinalIgnoreCase) ||
                controllerName == "user-roles" || controllerName == "userroles")
            {
                return controllerName switch
                {
                    "user" or "users" => ApiResponseMessages.Update.User.RoleSuccess,
                    "user-roles" or "userroles" => ApiResponseMessages.RolePermissions.UpdateSuccess,
                    _ => ApiResponseMessages.Update.Role.AssignmentSuccess
                };
            }

            // Permission updates
            if (actionName.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("claim", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Update.Role.PermissionsSuccess;
            }

            // Account-specific updates
            if (controllerName is "account" or "accounts" or "checkingaccount" or "savingsaccount")
            {
                return GetAccountSpecificUpdateMessage(actionName);
            }

            // Bank-specific updates
            if (controllerName is "bank" or "banks")
            {
                return GetBankSpecificUpdateMessage(actionName);
            }

            // Currency-specific updates
            if (controllerName is "currency" or "currencies")
            {
                return GetCurrencySpecificUpdateMessage(actionName);
            }

            return string.Empty; // No specific message found
        }

        /// <summary>
        /// Get account-specific update messages
        /// </summary>
        private static string GetAccountSpecificUpdateMessage(string actionName)
        {
            return actionName switch
            {
                var action when action.Contains("balance", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.BalanceSuccess,
                
                var action when action.Contains("limit", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.LimitsSuccess,
                
                var action when action.Contains("type", StringComparison.OrdinalIgnoreCase) ||
                                action.Contains("category", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.TypeSuccess,
                
                var action when action.Contains("interest", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.InterestSuccess,
                
                var action when action.Contains("overdraft", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.OverdraftSuccess,

                var action when action.Contains("beneficiary", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Account.BeneficiarySuccess,

                _ => string.Empty
            };
        }

        /// <summary>
        /// Get bank-specific update messages
        /// </summary>
        private static string GetBankSpecificUpdateMessage(string actionName)
        {
            return actionName switch
            {
                var action when action.Contains("branch", StringComparison.OrdinalIgnoreCase) ||
                                action.Contains("location", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Bank.BranchSuccess,
                
                var action when action.Contains("swift", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Bank.SwiftSuccess,
                
                var action when action.Contains("routing", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Bank.RoutingSuccess,

                var action when action.Contains("address", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Bank.AddressSuccess,

                _ => string.Empty
            };
        }

        /// <summary>
        /// Get currency-specific update messages
        /// </summary>
        private static string GetCurrencySpecificUpdateMessage(string actionName)
        {
            return actionName switch
            {
                var action when action.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                                action.Contains("exchange", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Currency.ExchangeRateSuccess,
                
                var action when action.Contains("symbol", StringComparison.OrdinalIgnoreCase) =>
                    ApiResponseMessages.Update.Currency.SymbolSuccess,

                _ => string.Empty
            };
        }

        /// <summary>
        /// Get process success message
        /// </summary>
        private string GetProcessMessage(string controllerName, string actionName)
        {
            // Authentication operations
            if (controllerName == "auth")
            {
                return actionName switch
                {
                    var action when action.Contains("login") => ApiResponseMessages.Authentication.LoginSuccess,
                    var action when action.Contains("logout") => ApiResponseMessages.Authentication.LogoutSuccess,
                    var action when action.Contains("refresh") => ApiResponseMessages.Authentication.TokenRefreshed,
                    var action when action.Contains("revoke") => ApiResponseMessages.Authentication.TokenRevoked,
                    _ => ApiResponseMessages.Authentication.OperationSuccess
                };
            }

            // Transaction operations
            if (controllerName is "transactions" or "accounttransactions")
            {
                return actionName switch
                {
                    var action when action.Contains("deposit") => ApiResponseMessages.Transaction.DepositSuccess,
                    var action when action.Contains("withdraw") => ApiResponseMessages.Transaction.WithdrawSuccess,
                    var action when action.Contains("transfer") => ApiResponseMessages.Transaction.TransferSuccess,
                    _ => ApiResponseMessages.Transaction.ProcessedSuccess
                };
            }

            // Role operations
            if (controllerName == "roleclaims")
            {
                return ApiResponseMessages.RolePermissions.AssignmentSuccess;
            }

            // User roles operations
            if (controllerName == "user-roles" || controllerName == "userroles")
            {
                return ApiResponseMessages.RolePermissions.UpdateSuccess;
            }

            return ApiResponseMessages.Processing.OperationSuccess;
        }

        /// <summary>
        /// Get status update message
        /// </summary>
        private string GetStatusMessage(string controllerName)
        {
            var isActiveParam = HttpContext.Request.Query["isActive"].FirstOrDefault();
            var isActivating = string.Equals(isActiveParam, "true", StringComparison.OrdinalIgnoreCase);

            return controllerName switch
            {
                "user" or "users" => isActivating ? 
                    ApiResponseMessages.Status.User.Activated : 
                    ApiResponseMessages.Status.User.Deactivated,
                "bank" or "banks" => isActivating ? 
                    ApiResponseMessages.Status.Bank.Activated : 
                    ApiResponseMessages.Status.Bank.Deactivated,
                "account" or "accounts" => isActivating ? 
                    ApiResponseMessages.Status.Account.Activated : 
                    ApiResponseMessages.Status.Account.Deactivated,
                "currency" or "currencies" => isActivating ? 
                    ApiResponseMessages.Status.Currency.Activated : 
                    ApiResponseMessages.Status.Currency.Deactivated,
                _ => isActivating ? 
                    ApiResponseMessages.Status.Generic.Activated : 
                    ApiResponseMessages.Status.Generic.Deactivated
            };
        }

        /// <summary>
        /// Get deactivate message
        /// </summary>
        private static string GetDeactivateMessage(string controllerName)
        {
            return controllerName switch
            {
                "user" or "users" => ApiResponseMessages.Delete.User.Deactivated,
                _ => ApiResponseMessages.Status.Generic.Deactivated
            };
        }

        /// <summary>
        /// Get controller name in lowercase
        /// </summary>
        private string GetControllerName()
        {
            return ControllerContext.ActionDescriptor.ControllerName?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Get action name in lowercase
        /// </summary>
        private string GetActionName()
        {
            return ControllerContext.ActionDescriptor.ActionName?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Log successful operation
        /// </summary>
        private void LogSuccess()
        {
            Logger.LogInformation("Operation completed successfully. Controller: {Controller}, Action: {Action}", 
                GetControllerName(), GetActionName());
        }

        /// <summary>
        /// Log failed operation
        /// </summary>
        private void LogFailure(IReadOnlyList<string> errors)
        {
            Logger.LogWarning("Operation failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                GetControllerName(), GetActionName(), string.Join(", ", errors));
        }

        /// <summary>
        /// Create appropriate error response based on error message patterns
        /// Enhanced to work seamlessly with semantic Result factory methods
        /// </summary>
        private IActionResult CreateErrorResponse(IReadOnlyList<string> errors)
        {
            if (errors.Count == 0)
                return BadRequest(new { success = false, message = ApiResponseMessages.Generic.UnknownError });

            var errorMessage = string.Join("; ", errors);
            var firstError = errors[0];

            // Map semantic Result error messages to proper HTTP status codes
            var statusCode = GetStatusCodeFromSemanticError(firstError);
            
            var errorResponse = new 
            { 
                success = false,
                errors = errors,
                message = errorMessage 
            };

            return statusCode switch
            {
                401 => Unauthorized(errorResponse),
                403 => StatusCode(403, errorResponse),
                404 => NotFound(errorResponse),
                409 => Conflict(errorResponse),
                422 => UnprocessableEntity(errorResponse),
                _ => BadRequest(errorResponse)
            };
        }

        /// <summary>
        /// Get HTTP status code from semantic error message patterns
        /// Optimized to work with Result factory method messages
        /// </summary>
        private static int GetStatusCodeFromSemanticError(string errorMessage)
        {
            // Direct matches for semantic Result factory methods
            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.NotAuthenticated, StringComparison.Ordinal))
                return 401;

            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AccessDenied, StringComparison.Ordinal))
                return 403;

            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.NotFound, StringComparison.Ordinal) ||
                errorMessage.Contains(ApiResponseMessages.ErrorPatterns.DoesNotExist, StringComparison.Ordinal))
                return 404;

            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AlreadyExists, StringComparison.Ordinal) ||
                errorMessage.Contains(ApiResponseMessages.ErrorPatterns.InsufficientFunds, StringComparison.Ordinal) ||
                errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AccountInactive, StringComparison.Ordinal))
                return 409;

            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.ValidationFailed, StringComparison.Ordinal) ||
                errorMessage.Contains(ApiResponseMessages.ErrorPatterns.BusinessRule, StringComparison.Ordinal))
                return 422;

            // Legacy pattern matching for backwards compatibility
            return GetLegacyStatusCode(errorMessage.ToLowerInvariant());
        }

        /// <summary>
        /// Legacy status code mapping for backwards compatibility
        /// </summary>
        private static int GetLegacyStatusCode(string errorMessage)
        {
            // Not Found (404)
            if (ContainsAny(errorMessage, "not found", "does not exist", "no longer exists"))
                return 404;

            // Unauthorized (401)
            if (ContainsAny(errorMessage, "not authenticated", "invalid credentials", "token expired", "invalid token"))
                return 401;

            // Forbidden (403)
            if (ContainsAny(errorMessage, "access denied", "insufficient permissions", "not authorized", "forbidden") ||
                (errorMessage.Contains("permission") && errorMessage.Contains("denied")))
                return 403;

            // Conflict (409)
            if (ContainsAny(errorMessage, "already exists", "duplicate", "conflict", "insufficient funds", "account is inactive", 
                "account is locked", "account is closed", "daily limit exceeded", "transaction limit") ||
                (errorMessage.Contains("balance") && errorMessage.Contains("insufficient")))
                return 409;

            // Unprocessable Entity (422)
            if (ContainsAny(errorMessage, "invalid amount", "validation failed", "business rule", "constraint violation", 
                "amount must be positive", "invalid transaction type"))
                return 422;

            // Default: Bad Request (400)
            return 400;
        }

        /// <summary>
        /// Check if string contains any of the specified values
        /// </summary>
        private static bool ContainsAny(string source, params string[] values)
        {
            return values.Any(value => source.Contains(value, StringComparison.Ordinal));
        }
    }
}
