using Tgs.BrandCertificate.Clients;

namespace TGS.Apis.OperatorApi.Middlewares;

internal class BrandFeature : IBrandFeature
{
	public Guid BrandId { get; init; }
}

public sealed class ReadBrandIdMiddleware(
	RequestDelegate next)
{
	private const string brandIdKey = "b-id";
	private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

	public async Task InvokeAsync(
		HttpContext context)
	{
		try
		{
			if (context.Request.Headers.ContainsKey(brandIdKey))
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