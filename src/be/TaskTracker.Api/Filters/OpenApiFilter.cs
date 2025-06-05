using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Xml.Linq;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Api.Filters;

public class OpenApiFilter(IConfiguration configuration) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Task Tracker API",
            Version = "v1",
            Description = "This document describes the Task Tracker API endpoints.",
            Contact = new OpenApiContact
            {
                Name = "Isaac Oselukwue",
                Email = "29353479@students.lincoln.ac.uk"
            }
        };
        document.Servers =
        [
            new OpenApiServer { Url = configuration["OpenApi:ServerUrl"]! }
        ];

        string xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
        XDocument? xmlDoc = null;
        if (File.Exists(xmlPath))
        {
            xmlDoc = XDocument.Load(xmlPath);
        }

        ProcessEnumSchemas(document);
        ApplyXmlDocumentation(document, xmlDoc);

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                operation.Value.Parameters ??= [];

                operation.Value.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Api-Key",
                    In = ParameterLocation.Header,
                    Required = false,
                    Description = "API Key required for authentication",
                    Example = new Microsoft.OpenApi.Any.OpenApiString("your-api-key-here")
                });
                operation.Value.Parameters.Add(new OpenApiParameter
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Required = false,
                    Description = "Bearer token required for authorization",
                    Example = new Microsoft.OpenApi.Any.OpenApiString("Bearer your-bearer-token-here")
                });

            }
        }
        return Task.CompletedTask;
    }

    private static void ProcessEnumSchemas(OpenApiDocument document)
    {
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                if (operation.Value.Parameters == null) continue;

                foreach (var parameter in operation.Value.Parameters)
                {
                    if (parameter.In == ParameterLocation.Query)
                    {
                        if (parameter.Name.Equals("Status", StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyEnumToParameter(parameter, typeof(StatusEnum));
                        }
                    }
                }
            }
        }
    }

    private static void ApplyEnumToParameter(OpenApiParameter parameter, Type enumType)
    {
        if (!enumType.IsEnum) return;

        parameter.Schema = new OpenApiSchema
        {
            Type = "string",
            Format = "enum",
            Enum = [.. Enum.GetNames(enumType).Select(name => new Microsoft.OpenApi.Any.OpenApiString(name))],
            Description = $"Possible values: {string.Join(", ", Enum.GetNames(enumType))}"
        };

        var firstValue = Enum.GetNames(enumType).FirstOrDefault();
        if (firstValue != null)
        {
            parameter.Example = new Microsoft.OpenApi.Any.OpenApiString(firstValue);
        }
    }

    private static void ApplyXmlDocumentation(OpenApiDocument document, XDocument? xmlDoc)
    {
        if (xmlDoc == null) return;

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                if (string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    (string controllersName, string actionName) = ParsePath(path.Key);

                    operation.Value.OperationId = $"{controllersName}_{actionName}";
                }
                string opId = operation.Value.OperationId;

                if (string.IsNullOrEmpty(opId)) continue;

                string controllerName = string.Empty;
                string methodName = opId;

                if (opId.Contains('_'))
                {
                    string[] parts = opId.Split('_');
                    if (parts.Length >= 2)
                    {
                        controllerName = parts[0];
                        methodName = parts[1];
                    }
                }

                // Search in XML doc for methods that match the operation
                var matchingMembers = xmlDoc.Descendants("member")
                .Where(m =>
                {
                    string memberName = m.Attribute("name")?.Value ?? "";
                    if (string.IsNullOrEmpty(memberName)) return false;

                    if (memberName.Contains($".{methodName}("))
                        return true;

                    // Controller+Action match
                    if (controllerName != null &&
                        memberName.Contains($"{controllerName}Controller") &&
                        memberName.Contains($".{methodName}("))
                        return true;

                    return memberName.Contains(methodName, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

                foreach (var member in matchingMembers)
                {
                    var summary = member.Element("summary");
                    if (summary != null)
                    {
                        operation.Value.Description = summary.Value.Trim();
                    }

                    var remarks = member.Element("remarks");
                    if (remarks != null)
                    {
                        if (!string.IsNullOrEmpty(operation.Value.Description))
                            operation.Value.Description += "\n\n";

                        operation.Value.Description += remarks.Value.Trim();
                    }

                    var requestBody = operation.Value.RequestBody;
                    if (requestBody != null)
                    {
                        var example = member.Element("example");
                        if (example != null)
                        {
                            foreach (var content in requestBody.Content.Values)
                            {
                                content.Example = new Microsoft.OpenApi.Any.OpenApiString(example.Value.Trim());
                            }
                        }
                    }

                    var paramElements = member.Elements("param");
                    foreach (var paramElement in paramElements)
                    {
                        string? paramName = paramElement.Attribute("name")?.Value;
                        if (string.IsNullOrEmpty(paramName)) continue;

                        var parameter = operation.Value.Parameters
                            .FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

                        if (parameter != null)
                        {
                            parameter.Description = paramElement.Value.Trim();
                        }
                    }

                    var responseElements = member.Elements("response");
                    foreach (var responseElement in responseElements)
                    {
                        string? code = responseElement.Attribute("code")?.Value;
                        if (string.IsNullOrEmpty(code)) continue;

                        if (!operation.Value.Responses.TryGetValue(code, out var response))
                        {
                            response = new OpenApiResponse();
                            operation.Value.Responses[code] = response;
                        }

                        response.Description = responseElement.Value.Trim();
                    }
                }
            }
        }
    }
    private static (string controllersName, string actionName) ParsePath(string path)
    {
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        int startIndex = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i].ToLowerInvariant();
            if (segment == "api" || segment.StartsWith('v'))
            {
                startIndex++;
                continue;
            }
            break;
        }
        string controllersName = string.Empty;

        if (startIndex < segments.Length)
        {
            controllersName = segments[startIndex];
        }
        string actionName = string.Empty;

        if (startIndex + 1 < segments.Length)
        {
            string action = segments[startIndex + 1];
            if (action.Contains('-'))
            {
                var parts = action.Split('-');
                action = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        action += char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
                    }
                }
            }
            actionName = action;
        }

        return (controllersName, actionName);
    }
}