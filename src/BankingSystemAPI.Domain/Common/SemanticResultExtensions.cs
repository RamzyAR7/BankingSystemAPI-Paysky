using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Extension methods to create semantic Result objects using ApiResponseMessages constants
    /// This provides a bridge between the Result factory methods and consistent error messages
    /// </summary>
    public static class SemanticResultExtensions
    {
        /// <summary>
        /// Creates a NotFound result using consistent message format
        /// </summary>
        /// <param name="entityName">Name of the entity (e.g., "User", "Account")</param>
        /// <param name="id">ID of the entity that was not found</param>
        /// <returns>Result with 404 status code mapping</returns>
        public static Result NotFoundEntity(string entityName, object id)
        {
            var message = string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, entityName, id);
            return Result.NotFound(message);
        }

        /// <summary>
        /// Creates a NotFound result for generic entity using consistent message format
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <param name="entityName">Name of the entity (e.g., "User", "Account")</param>
        /// <param name="id">ID of the entity that was not found</param>
        /// <returns>Result<T> with 404 status code mapping</returns>
        public static Result<T> NotFoundEntity<T>(string entityName, object id)
        {
            var message = string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, entityName, id);
            return Result<T>.NotFound(message);
        }

        /// <summary>
        /// Creates an AlreadyExists result using consistent message format
        /// </summary>
        /// <param name="entityName">Name of the entity (e.g., "User", "Account")</param>
        /// <param name="identifier">Identifier that already exists (e.g., email, account number)</param>
        /// <returns>Result with 409 status code mapping</returns>
        public static Result EntityAlreadyExists(string entityName, string identifier)
        {
            var message = string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, entityName, identifier);
            return Result.AlreadyExists(entityName, identifier);
        }

        /// <summary>
        /// Creates an AlreadyExists result for generic entity using consistent message format
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <param name="entityName">Name of the entity (e.g., "User", "Account")</param>
        /// <param name="identifier">Identifier that already exists (e.g., email, account number)</param>
        /// <returns>Result<T> with 409 status code mapping</returns>
        public static Result<T> EntityAlreadyExists<T>(string entityName, string identifier)
        {
            var message = string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, entityName, identifier);
            return Result<T>.AlreadyExists(entityName, identifier);
        }

        /// <summary>
        /// Creates an InsufficientFunds result using consistent message format
        /// </summary>
        /// <param name="requested">Amount requested</param>
        /// <param name="available">Amount available</param>
        /// <returns>Result with 409 status code mapping</returns>
        public static Result InsufficientFundsError(decimal requested, decimal available)
        {
            return Result.InsufficientFunds(requested, available);
        }

        /// <summary>
        /// Creates an InsufficientFunds result for generic entity using consistent message format
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <param name="requested">Amount requested</param>
        /// <param name="available">Amount available</param>
        /// <returns>Result<T> with 409 status code mapping</returns>
        public static Result<T> InsufficientFundsError<T>(decimal requested, decimal available)
        {
            return Result<T>.InsufficientFunds(requested, available);
        }

        /// <summary>
        /// Creates an AccountInactive result using consistent message format
        /// </summary>
        /// <param name="accountNumber">Account number that is inactive</param>
        /// <returns>Result with 409 status code mapping</returns>
        public static Result AccountInactiveError(string accountNumber)
        {
            return Result.AccountInactive(accountNumber);
        }

        /// <summary>
        /// Creates an AccountInactive result for generic entity using consistent message format
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <param name="accountNumber">Account number that is inactive</param>
        /// <returns>Result<T> with 409 status code mapping</returns>
        public static Result<T> AccountInactiveError<T>(string accountNumber)
        {
            return Result<T>.AccountInactive(accountNumber);
        }

        /// <summary>
        /// Creates an Unauthorized result using consistent message
        /// </summary>
        /// <returns>Result with 401 status code mapping</returns>
        public static Result NotAuthenticatedError()
        {
            return Result.Unauthorized(ApiResponseMessages.ErrorPatterns.NotAuthenticated);
        }

        /// <summary>
        /// Creates an Unauthorized result for generic entity using consistent message
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <returns>Result<T> with 401 status code mapping</returns>
        public static Result<T> NotAuthenticatedError<T>()
        {
            return Result<T>.Unauthorized(ApiResponseMessages.ErrorPatterns.NotAuthenticated);
        }

        /// <summary>
        /// Creates a Forbidden result using consistent message
        /// </summary>
        /// <returns>Result with 403 status code mapping</returns>
        public static Result AccessDeniedError()
        {
            return Result.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied);
        }

        /// <summary>
        /// Creates a Forbidden result for generic entity using consistent message
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <returns>Result<T> with 403 status code mapping</returns>
        public static Result<T> AccessDeniedError<T>()
        {
            return Result<T>.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied);
        }

        /// <summary>
        /// Creates an InvalidCredentials result using consistent message
        /// </summary>
        /// <returns>Result with 401 status code mapping</returns>
        public static Result InvalidCredentialsError()
        {
            return Result.InvalidCredentials(ApiResponseMessages.ErrorPatterns.InvalidCredentials);
        }

        /// <summary>
        /// Creates an InvalidCredentials result for generic entity using consistent message
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <returns>Result<T> with 401 status code mapping</returns>
        public static Result<T> InvalidCredentialsError<T>()
        {
            return Result<T>.InvalidCredentials(ApiResponseMessages.ErrorPatterns.InvalidCredentials);
        }

        /// <summary>
        /// Creates a TokenExpired result using consistent message
        /// </summary>
        /// <returns>Result with 401 status code mapping</returns>
        public static Result TokenExpiredError()
        {
            return Result.TokenExpired(ApiResponseMessages.ErrorPatterns.TokenExpired);
        }

        /// <summary>
        /// Creates a TokenExpired result for generic entity using consistent message
        /// </summary>
        /// <typeparam name="T">Return type for the result</typeparam>
        /// <returns>Result<T> with 401 status code mapping</returns>
        public static Result<T> TokenExpiredError<T>()
        {
            return Result<T>.TokenExpired(ApiResponseMessages.ErrorPatterns.TokenExpired);
        }
    }
}