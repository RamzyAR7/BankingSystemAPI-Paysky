#region Usings
using System.Net;
using BankingSystemAPI.Domain.Constant;
#endregion

namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Centralized mapping between ErrorType and HTTP status codes.
    /// Keeps HTTP mapping consolidated and easy to change/test.
    /// </summary>
    public static class ResultErrorMapper
    {
        public static int MapToStatusCode(ErrorType type) => type switch
        {
            ErrorType.Validation => (int)HttpStatusCode.UnprocessableEntity, // 422
            ErrorType.Conflict => (int)HttpStatusCode.Conflict, // 409
            ErrorType.Unauthorized => (int)HttpStatusCode.Unauthorized, // 401
            ErrorType.Forbidden => (int)HttpStatusCode.Forbidden, // 403
            ErrorType.NotFound => (int)HttpStatusCode.NotFound, // 404
            ErrorType.BusinessRule => (int)HttpStatusCode.Conflict, // business rule -> 409 by default
            _ => (int)HttpStatusCode.BadRequest // Unknown -> 400
        };
    }
}