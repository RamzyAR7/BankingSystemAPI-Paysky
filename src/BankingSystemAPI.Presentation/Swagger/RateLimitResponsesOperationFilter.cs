using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace BankingSystemAPI.Presentation.Swagger
{
    public class RateLimitResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor?.EndpointMetadata;
            if (endpointMetadata == null) return;

            // Skip if AllowAnonymous present
            var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
            if (hasAllowAnonymous) return;

            // detect EnableRateLimiting attribute in endpoint metadata
            var hasEnableRateLimiting = endpointMetadata.Any(m => m.GetType().FullName == "Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute"
                                                                    || m.GetType().Name.Contains("EnableRateLimiting"));

            if (hasEnableRateLimiting)
            {
                if (!operation.Responses.ContainsKey("429"))
                {
                    operation.Responses.Add("429", new OpenApiResponse { Description = "Too many requests" });
                }
            }
        }
    }
}
