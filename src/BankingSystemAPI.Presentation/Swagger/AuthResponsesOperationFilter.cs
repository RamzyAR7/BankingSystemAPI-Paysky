#region Usings
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
#endregion


namespace BankingSystemAPI.Presentation.Swagger
{
    public class AuthResponsesOperationFilter : IOperationFilter
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Use endpoint metadata to detect authorization and allow-anonymous
            var endpointMetadata = context.ApiDescription.ActionDescriptor?.EndpointMetadata;
            var hasAllowAnonymous = endpointMetadata?.Any(m => m is IAllowAnonymous) ?? false;

            if (hasAllowAnonymous)
            {
                return; // no auth responses for anonymous endpoints
            }

            var hasAuthorize = endpointMetadata?.Any(m => m is IAuthorizeData) ?? false;
            // also consider custom permission attributes by name
            var hasPermissionFilter = endpointMetadata?.Any(m => m.GetType().Name.Contains("Permission")) ?? false;

            if (hasAuthorize || hasPermissionFilter)
            {
                if (!operation.Responses.ContainsKey("401"))
                {
                    operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                }
                if (!operation.Responses.ContainsKey("403"))
                {
                    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                }
            }
        }
    }
}

