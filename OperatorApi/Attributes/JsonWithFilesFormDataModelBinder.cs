using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace TGS.Apis.OperatorApi.Attributes;

public class JsonWithFilesFormDataModelBinder(IOptions<JsonOptions> jsonOptionsAccessor) : IModelBinder
{
	private readonly JsonSerializerOptions _serializerOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;

	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		ArgumentNullException.ThrowIfNull(bindingContext);

		var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
		if (valueResult == ValueProviderResult.None)
		{
			var message = bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
			_ = bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
			return;
		}

		var rawValue = valueResult.FirstValue;

		var model = JsonSerializer.Deserialize(rawValue, bindingContext.ModelType, _serializerOptions);

		foreach (var property in bindingContext.ModelMetadata.Properties)
		{
			if (property.ModelType != typeof(IFormFile))
				continue;

			var fieldName = property.BinderModelName ?? property.PropertyName;
			var modelName = fieldName;
			var propertyModel = property.PropertyGetter(bindingContext.Model);
			ModelBindingResult propertyResult;
			using (bindingContext.EnterNestedScope(property, fieldName, modelName, propertyModel))
			{
				await BindModelAsync(bindingContext);
				propertyResult = bindingContext.Result;
			}

			if (propertyResult.IsModelSet)
			{
				property.PropertySetter(model, propertyResult.Model);
			}
			else if (property.IsBindingRequired)
			{
				var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
				_ = bindingContext.ModelState.TryAddModelError(modelName, message);
			}
		}

		bindingContext.Result = ModelBindingResult.Success(model);
	}
}
