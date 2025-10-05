#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public static class BankGuard
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        /// <summary>
        /// Validates that both banks are the same for cross-bank operations
        /// </summary>
        /// <param name="actingBankId">The acting user's bank ID</param>
        /// <param name="targetBankId">The target resource's bank ID</param>
        /// <returns>Result indicating success or failure with error message</returns>
        public static Result ValidateSameBank(int? actingBankId, int? targetBankId)
        {
            var actingValidation = ValidateBankId(actingBankId, "Acting user");
            if (actingValidation.IsFailure)
                return actingValidation;

            var targetValidation = ValidateBankId(targetBankId, "Target resource");
            if (targetValidation.IsFailure)
                return targetValidation;

            return actingBankId != targetBankId 
                ? Result.Forbidden("Access forbidden due to bank isolation policy.")
                : Result.Success();
        }

        /// <summary>
        /// Validates bank access for a single entity using functional composition
        /// </summary>
        /// <param name="userBankId">User's bank ID</param>
        /// <param name="entityBankId">Entity's bank ID</param>
        /// <param name="entityType">Type of entity for error message</param>
        /// <returns>Result indicating success or failure</returns>
        public static Result ValidateBankAccess(int? userBankId, int? entityBankId, string entityType = "resource")
        {
            var userValidation = ValidateUserBankId(userBankId);
            if (userValidation.IsFailure)
                return userValidation;

            var entityValidation = ValidateEntityBankId(entityBankId, entityType);
            if (entityValidation.IsFailure)
                return entityValidation;

            return userBankId != entityBankId
                ? Result.Forbidden($"Access to {entityType} from different bank is forbidden.")
                : Result.Success();
        }

        /// <summary>
        /// Validates multiple bank accesses and combines results
        /// </summary>
        /// <param name="userBankId">User's bank ID</param>
        /// <param name="entityBankIds">Collection of entity bank IDs to validate</param>
        /// <param name="entityType">Type of entities for error messages</param>
        /// <returns>Combined result of all validations</returns>
        public static Result ValidateMultipleBankAccess(int? userBankId, IEnumerable<int?> entityBankIds, string entityType = "resource")
        {
            var userValidation = ValidateUserBankId(userBankId);
            if (userValidation.IsFailure)
                return userValidation;

            var entityValidations = entityBankIds
                .Select(entityBankId => ValidateBankAccess(userBankId, entityBankId, entityType))
                .ToArray();

            return ResultExtensions.ValidateAll(entityValidations);
        }

        /// <summary>
        /// Validates that a bank ID is not null with context-specific error message
        /// </summary>
        private static Result ValidateBankId(int? bankId, string context)
        {
            return bankId.HasValue
                ? Result.Success()
                : Result.BadRequest($"{context} must belong to a bank.");
        }

        /// <summary>
        /// Validates user bank ID specifically
        /// </summary>
        private static Result ValidateUserBankId(int? userBankId)
        {
            return userBankId.HasValue
                ? Result.Success()
                : Result.Unauthorized("User must belong to a bank to access this resource.");
        }

        /// <summary>
        /// Validates entity bank ID specifically
        /// </summary>
        private static Result ValidateEntityBankId(int? entityBankId, string entityType)
        {
            return entityBankId.HasValue
                ? Result.Success()
                : Result.BadRequest($"The {entityType} does not belong to any bank.");
        }
    }
}

