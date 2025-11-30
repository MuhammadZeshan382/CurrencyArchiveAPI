using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace CurrencyArchiveAPI.Documentation;

/// <summary>
/// Swagger operation filter that applies API documentation from external JSON file.
/// </summary>
public class ApiDocumentationFilter : IOperationFilter
{
    private readonly Dictionary<string, Dictionary<string, EndpointDocumentation>> _documentation;

    public ApiDocumentationFilter()
    {
        _documentation = LoadDocumentation();
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.MethodInfo.DeclaringType?.Name;
        var actionName = context.MethodInfo.Name;

        if (controllerName == null || actionName == null)
            return;

        // Try to get documentation for this controller and action
        if (_documentation.TryGetValue(controllerName, out var controllerDocs) &&
            controllerDocs.TryGetValue(actionName, out var endpointDoc))
        {
            // Apply summary and description
            operation.Summary = endpointDoc.Summary;
            operation.Description = endpointDoc.Description;

            // Apply parameter descriptions
            if (endpointDoc.Parameters != null && operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters)
                {
                    if (endpointDoc.Parameters.TryGetValue(parameter.Name, out var paramDescription))
                    {
                        parameter.Description = paramDescription;
                    }
                }
            }

            // Apply response descriptions
            if (endpointDoc.Responses != null)
            {
                foreach (var responseDoc in endpointDoc.Responses)
                {
                    if (operation.Responses.TryGetValue(responseDoc.Key, out var response))
                    {
                        response.Description = responseDoc.Value;
                    }
                }
            }
        }
    }

    private Dictionary<string, Dictionary<string, EndpointDocumentation>> LoadDocumentation()
    {
        try
        {
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Documentation", "ApiDocumentation.json");
            
            if (!File.Exists(jsonPath))
            {
                // Try relative path during development
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Documentation", "ApiDocumentation.json");
            }

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Warning: API documentation file not found at {jsonPath}");
                return new Dictionary<string, Dictionary<string, EndpointDocumentation>>();
            }

            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, EndpointDocumentation>>>(json, options)
                   ?? new Dictionary<string, Dictionary<string, EndpointDocumentation>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading API documentation: {ex.Message}");
            return new Dictionary<string, Dictionary<string, EndpointDocumentation>>();
        }
    }
}

/// <summary>
/// Represents documentation for a single API endpoint.
/// </summary>
public class EndpointDocumentation
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
    public Dictionary<string, string>? Responses { get; set; }
}
