using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Interfaces;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Filters;

public class ErrorMessageMapperFilter(IErrorMapper errorMapper, ILogger<ErrorMessageMapperFilter> logger) : IAsyncExceptionFilter
{
	private readonly IErrorMapper _errorMapper = errorMapper ?? throw new System.ArgumentNullException(nameof(errorMapper));
	private readonly ILogger<ErrorMessageMapperFilter> _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

	public Task OnExceptionAsync(ExceptionContext context)
	{
		var exception = context.Exception;

		var error = _errorMapper.GetApiErrorByException(exception);
		var statusCode = _errorMapper.GetHttpStatusByException(exception);
		var message = exception.GetMessageChain();
		var messageKey = _errorMapper.GetMessageKeyByException(exception);

		if (statusCode >= 500)
			_logger.LogError(exception, "Caught global error!");
		else
			_logger.LogWarning(exception, "Caught global exception!");

		var responseFeature = context.HttpContext.Features.Get<IHttpResponseFeature>();

		if (responseFeature is not null)
			responseFeature.StatusCode = statusCode;

		context.Result = new ObjectResult(new TgsResponse
		{
			Error = error,
			ErrorMessage = message,
			ErrorMessageKey = messageKey
		});

		context.ExceptionHandled = true;

		return Task.CompletedTask;
	}
}
