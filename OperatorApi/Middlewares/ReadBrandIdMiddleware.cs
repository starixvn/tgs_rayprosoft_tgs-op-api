#nullable enable

using System.Linq;
using Tgs.BrandCertificate.Clients;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Core.DataServices.Tgs.Models;

namespace TGS.Apis.OperatorApi.Middlewares;

internal class BrandFeature : IBrandFeature
{
	public Guid BrandId { get; init; }
}

public sealed class ReadBrandIdMiddleware(
	RequestDelegate next)
{
	private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
	const string brandIdKey = "b-id";

	public async Task InvokeAsync(
		HttpContext context)
	{
		try
		{
			if (context.Items.GetBrandId() == Guid.Empty && context.Request.Headers.ContainsKey(brandIdKey))
			{
				var brandId = context.Request.Headers[brandIdKey];
				context.Features.Set<IBrandFeature>(new BrandFeature
				{
					BrandId = new Guid(brandId.ToString())
				});
			}
		}
		finally
		{
			await _next(context).ConfigureAwait(false);
		}
	}
}