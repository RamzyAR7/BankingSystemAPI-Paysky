using BankingSystemAPI.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public static class BankGuard
    {
        /// <summary>
        /// Validates that both banks are the same for cross-bank operations
        /// </summary>
        /// <param name="actingBankId">The acting user's bank ID</param>
        /// <param name="targetBankId">The target resource's bank ID</param>
        /// <returns>Result indicating success or failure with error message</returns>
        public static Result ValidateSameBank(int? actingBankId, int? targetBankId)
        {
            if (actingBankId == null || targetBankId == null || actingBankId != targetBankId)
                return Result.Forbidden("Access forbidden due to bank isolation policy.");
            
            return Result.Success();
        }

        /// <summary>
        /// Validates bank access for a single entity
        /// </summary>
        /// <param name="userBankId">User's bank ID</param>
        /// <param name="entityBankId">Entity's bank ID</param>
        /// <param name="entityType">Type of entity for error message</param>
        /// <returns>Result indicating success or failure</returns>
        public static Result ValidateBankAccess(int? userBankId, int? entityBankId, string entityType = "resource")
        {
            if (userBankId == null)
                return Result.Unauthorized("User must belong to a bank to access this resource.");

            if (entityBankId == null)
                return Result.BadRequest($"The {entityType} does not belong to any bank.");

            if (userBankId != entityBankId)
                return Result.Forbidden($"Access to {entityType} from different bank is forbidden.");

            return Result.Success();
        }

        /// <summary>
        /// Legacy method for backward compatibility - will be removed in future versions
        /// </summary>
        [Obsolete("Use ValidateSameBank method instead. This method will be removed in future versions.")]
        public static void EnsureSameBank(int? actingBankId, int? targetBankId)
        {
            var result = ValidateSameBank(actingBankId, targetBankId);
            if (result.IsFailure)
                throw new Application.Exceptions.ForbiddenException(result.ErrorMessage);
        }
    }
}
