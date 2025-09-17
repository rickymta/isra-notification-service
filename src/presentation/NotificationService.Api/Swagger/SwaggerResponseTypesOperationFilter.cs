using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;

namespace NotificationService.Api.Swagger;

/// <summary>
/// Operation filter to add standard response types to Swagger documentation
/// </summary>
public class SwaggerResponseTypesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add common error responses if they don't exist
        if (!operation.Responses.ContainsKey("400"))
        {
            operation.Responses.Add("400", new OpenApiResponse
            {
                Description = "Bad Request - Invalid input parameters"
            });
        }

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse
            {
                Description = "Unauthorized - Authentication required"
            });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse
            {
                Description = "Forbidden - Insufficient permissions"
            });
        }

        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Internal Server Error - An error occurred processing the request"
            });
        }

        // Add rate limiting response for specific endpoints
        if (context.MethodInfo.DeclaringType?.Name.Contains("Controller") == true)
        {
            if (!operation.Responses.ContainsKey("429"))
            {
                operation.Responses.Add("429", new OpenApiResponse
                {
                    Description = "Too Many Requests - Rate limit exceeded"
                });
            }
        }
    }
}
