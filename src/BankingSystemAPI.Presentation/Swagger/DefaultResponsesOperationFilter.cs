using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BankingSystemAPI.Presentation.Swagger
{
    /// <summary>
    /// Adds common response codes to operations when they are not already documented.
    /// Useful for documenting 400/404/409/500 responses across the API.
    /// </summary>
    public class DefaultResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add 400 Bad Request for endpoints that accept a body (POST/PUT/PATCH)
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
            if ((method == "POST" || method == "PUT" || method == "PATCH") && !operation.Responses.ContainsKey("400"))
            {
                operation.Responses.Add("400", new OpenApiResponse { Description = "Bad Request - validation error or invalid input" });
            }

            // Add 404 Not Found for endpoints that have route parameters (likely resource by id) if not present
            if (context.ApiDescription.RelativePath != null && context.ApiDescription.RelativePath.Contains("{") && !operation.Responses.ContainsKey("404"))
            {
                operation.Responses.Add("404", new OpenApiResponse { Description = "Not Found - resource not found" });
            }

            // Add 409 Conflict for endpoints that may return conflicts (commonly POST/PUT)
            if ((method == "POST" || method == "PUT" || method == "PATCH") && !operation.Responses.ContainsKey("409"))
            {
                operation.Responses.Add("409", new OpenApiResponse { Description = "Conflict - resource state conflict or concurrency error" });
            }

            // Add 500 Internal Server Error as a catch-all for unhandled exceptions
            if (!operation.Responses.ContainsKey("500"))
            {
                operation.Responses.Add("500", new OpenApiResponse { Description = "Internal Server Error" });
            }
        }
    }
}
