using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BankingSystemAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Authentication response data returned after successful login/refresh.
    /// </summary>
    public class AuthResDto
    {
        /// <summary>
        /// Optional message to return to the client.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Indicates whether authentication succeeded.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Authenticated username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// User email.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Roles assigned to the user.
        /// </summary>
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Access token (JWT) to be used for authenticated requests.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// When the access token expires.
        /// </summary>
        public DateTime? ExpiresOn { get; set; }

        /// <summary>
        /// Refresh token used to obtain new access tokens. This property is ignored in Swagger output.
        /// </summary>
        [JsonIgnore]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Expiration time of the refresh token.
        /// </summary>
        public DateTime RefreshTokenExpiration { get; set; }

        /// <summary>
        /// Absolute expiration time for authentication session.
        /// </summary>
        public DateTime AbsoluteExpiresOn { get; set; }
    }
}
