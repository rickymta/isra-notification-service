using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NotificationService.Api.Swagger;

/// <summary>
/// Document filter to customize the overall Swagger documentation
/// </summary>
public class CustomDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add custom servers
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new() { Url = "https://localhost:7001", Description = "Development HTTPS" },
            new() { Url = "http://localhost:5001", Description = "Development HTTP" },
            new() { Url = "https://api.example.com", Description = "Production" }
        };

        // Add external documentation
        swaggerDoc.ExternalDocs = new OpenApiExternalDocs
        {
            Description = "Notification Service Documentation",
            Url = new Uri("https://github.com/rickymta/isra-notification-service/blob/master/README.md")
        };

        // Sort paths alphabetically
        var sortedPaths = swaggerDoc.Paths
            .OrderBy(p => p.Key)
            .ToDictionary(p => p.Key, p => p.Value);
        
        swaggerDoc.Paths.Clear();
        foreach (var path in sortedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }

        // Add common response schemas
        if (!swaggerDoc.Components.Schemas.ContainsKey("ErrorResponse"))
        {
            swaggerDoc.Components.Schemas.Add("ErrorResponse", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["message"] = new() { Type = "string", Description = "Error message" },
                    ["code"] = new() { Type = "string", Description = "Error code" },
                    ["details"] = new() { Type = "string", Description = "Detailed error information" },
                    ["timestamp"] = new() { Type = "string", Format = "date-time", Description = "Error timestamp" }
                }
            });
        }

        if (!swaggerDoc.Components.Schemas.ContainsKey("SuccessResponse"))
        {
            swaggerDoc.Components.Schemas.Add("SuccessResponse", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["message"] = new() { Type = "string", Description = "Success message" },
                    ["timestamp"] = new() { Type = "string", Format = "date-time", Description = "Response timestamp" }
                }
            });
        }
    }
}
