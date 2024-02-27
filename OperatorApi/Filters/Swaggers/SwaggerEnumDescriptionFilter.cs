using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TGS.Apis.OperatorApi.Filters.Swaggers;

public class SwaggerEnumDescriptionFilter : ISchemaFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		var type = context.Type;
		if (!type.IsEnum)
		{
			return;
		}

		var extension = new OpenApiObject();

		foreach (int v in Enum.GetValues(type))
		{
			var name = Enum.GetName(type, v);
			extension[name] = new OpenApiInteger(v);
		}

		schema.Extensions.Add(type.Name, extension);
	}
}