using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TGS.Apis.OperatorApi.Models;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Attributes;

public class TgsResponseWrapperAttribute : ActionFilterAttribute
{
	public override void OnResultExecuting(ResultExecutingContext context)
	{
		if (!context.ModelState.IsValid)
			return;

		switch (context.Result)
		{
			case EmptyResult:
				context.Result = new ObjectResult(new TgsResponse());
				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<object> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<int> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<long> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<float> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<double> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<decimal> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse and IAsyncEnumerable<Guid> ae:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = ae.ToEnumerable()
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;

			case ObjectResult result when result.Value is not TgsResponse:
				{
					var adjustResult = new ObjectResult(new SuccessResponse<object>
					{
						Data = result.Value
					})
					{
						StatusCode = result.StatusCode,
						Formatters = result.Formatters
					};

					context.Result = adjustResult;
				}

				break;
		}
	}
}
