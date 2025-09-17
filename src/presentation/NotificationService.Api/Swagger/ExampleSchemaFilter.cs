using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NotificationService.Api.Models;
using NotificationService.Domain.Enums;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NotificationService.Api.Swagger;

/// <summary>
/// Schema filter to add examples to Swagger documentation
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(SendNotificationRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["templateId"] = new OpenApiString("email-welcome-template"),
                ["channel"] = new OpenApiString("Email"),
                ["recipient"] = new OpenApiObject
                {
                    ["userId"] = new OpenApiString("user-123"),
                    ["email"] = new OpenApiString("user@example.com"),
                    ["language"] = new OpenApiString("en")
                },
                ["variables"] = new OpenApiObject
                {
                    ["userName"] = new OpenApiString("John Doe"),
                    ["companyName"] = new OpenApiString("Example Corp")
                },
                ["priority"] = new OpenApiInteger(2),
                ["metadata"] = new OpenApiObject
                {
                    ["source"] = new OpenApiString("user-registration"),
                    ["campaign"] = new OpenApiString("welcome-series")
                }
            };
        }
        else if (context.Type == typeof(CreateInAppNotificationRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["userId"] = new OpenApiString("user-123"),
                ["title"] = new OpenApiString("Welcome to our platform!"),
                ["message"] = new OpenApiString("Thank you for joining us. Get started by exploring our features."),
                ["type"] = new OpenApiString("welcome"),
                ["priority"] = new OpenApiString("normal"),
                ["data"] = new OpenApiObject
                {
                    ["actionUrl"] = new OpenApiString("/dashboard"),
                    ["buttonText"] = new OpenApiString("Get Started")
                },
                ["expiresAt"] = new OpenApiString("2024-12-31T23:59:59Z"),
                ["tags"] = new OpenApiArray
                {
                    new OpenApiString("welcome"),
                    new OpenApiString("onboarding")
                }
            };
        }
        else if (context.Type == typeof(NotificationChannel))
        {
            schema.Example = new OpenApiString("Email");
        }
        else if (context.Type == typeof(NotificationStatus))
        {
            schema.Example = new OpenApiString("Sent");
        }
    }
}
