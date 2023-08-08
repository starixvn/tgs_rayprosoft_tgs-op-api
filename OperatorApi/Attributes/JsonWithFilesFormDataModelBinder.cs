using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Options;

namespace TGS.Apis.OperatorApi.Attributes;

public class JsonWithFilesFormDataModelBinder : IModelBinder
{
	private readonly FormFileModelBinder _formFileModelBinder;
	private readonly JsonSerializerOptions _serializerOptions;

	public JsonWithFilesFormDataModelBinder(IOptions<JsonOptions> jsonOptionsAccessor, ILoggerFactory loggerFactory)
	{
		_serializerOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;
		_formFileModelBinder = new FormFileModelBinder(loggerFactory);
	}

	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext == null)
			throw new ArgumentNullException(nameof(bindingContext));

		var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
		if (valueResult == ValueProviderResult.None)
		{
			var message = bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
			_ = bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
			return;
		}

		var model = JsonSerializer.Deserialize(valueResult.FirstValue, bindingContext.ModelType, _serializerOptions);

		foreach (var property in bindingContext.ModelMetadata.Properties)
		{
			if (property.ModelType != typeof(IFormFile))
				continue;

			var fieldName = property.BinderModelName ?? property.PropertyName;

			var propertyModel = property.PropertyGetter(bindingContext.Model);
			ModelBindingResult propertyResult;
			using (bindingContext.EnterNestedScope(property, fieldName, fieldName, propertyModel))
			{
				await _formFileModelBinder.BindModelAsync(bindingContext);
				propertyResult = bindingContext.Result;
			}

			if (propertyResult.IsModelSet)
			{
				property.PropertySetter(model, propertyResult.Model);
			}
			else if (property.IsBindingRequired)
			{
				var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
				_ = bindingContext.ModelState.TryAddModelError(fieldName, message);
			}
		}

		bindingContext.Result = ModelBindingResult.Success(model);
	}
}
