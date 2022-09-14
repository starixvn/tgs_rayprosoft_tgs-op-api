using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TGS.Apis.OperatorApi.Models;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Filters.Swaggers;

public class SwaggerTgsResponseWrapperFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var response in operation.Responses)
        {
            var resType = context.ApiDescription.SupportedResponseTypes
                .Where(r => r.StatusCode.ToString() == response.Key)
                .FirstOrDefault();

            foreach (var content in response.Value.Content)
            {
                if (resType != null && !resType.Type.IsSubclassOf(typeof(TgsResponse)))
                {
                    var type = typeof(SuccessResponse<>).MakeGenericType(resType.Type);
                    var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

                    content.Value.Schema = schema;
                }
            }
        }
    }
}
