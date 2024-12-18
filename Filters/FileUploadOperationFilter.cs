// src/BotGarden.Web/Filters/FileUploadOperationFilter.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BotGarden.Web.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the action has any IFormFile parameters
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                            p.ParameterType == typeof(IEnumerable<IFormFile>))
                .ToList();

            if (!fileParameters.Any())
                return; // No file parameters, no need to modify the operation

            // Remove the file parameters from the operation's parameters
            foreach (var param in fileParameters)
            {
                var existingParam = operation.Parameters.FirstOrDefault(p => p.Name == param.Name);
                if (existingParam != null)
                {
                    operation.Parameters.Remove(existingParam);
                }
            }

            // Define a new RequestBody with multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParameters.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }),
                            Required = new HashSet<string>(fileParameters.Select(p => p.Name))
                        }
                    }
                },
                Required = true
            };

            // Optionally, set the operation's consumes to multipart/form-data
            // Not necessary in OpenAPI 3.0, as content types are specified in Content
        }
    }
}
