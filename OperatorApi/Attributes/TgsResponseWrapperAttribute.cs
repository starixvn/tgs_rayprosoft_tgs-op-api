using System;
using System.Collections.Generic;
using System.Linq;
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

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<object> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<int> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<long> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<float> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<double> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<decimal> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse && result.Value is IAsyncEnumerable<Guid> ae:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = ae.ToEnumerable()
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;

            case ObjectResult result when result.Value is not TgsResponse:
                {
                    var adjustResult = new ObjectResult(new SuccessResponse<object>
                    {
                        Data = result.Value
                    });

                    adjustResult.StatusCode = result.StatusCode;
                    adjustResult.Formatters = result.Formatters;

                    context.Result = adjustResult;
                }
                break;
        }
    }
}
