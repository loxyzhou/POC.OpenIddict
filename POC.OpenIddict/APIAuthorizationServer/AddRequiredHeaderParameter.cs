namespace APIAuthorizationServer
{
    using System.Collections.Generic;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();           

            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = "Content-Type",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                },
                Description = "The Content-Type header",
                Example = new OpenApiString("application/x-www-form-urlencoded")
            });
        }
    }
}
