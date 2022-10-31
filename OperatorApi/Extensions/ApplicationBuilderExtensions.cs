using TGS.Apis.OperatorApi.Middlewares;

namespace TGS.Apis.OperatorApi.Extensions;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseVerifyJwtTokenMiddlewares(this IApplicationBuilder app,
		string[] bypassRouting)
	{
		var bypass = bypassRouting.Concat(new[] { "/api/auth" }).Distinct().ToArray();

		_ = app.UseWhen(context => bypass.All(r => !context.Request.Path.StartsWithSegments(r)),
			builder => builder
				.UseMiddleware<VerifyCredentialsMiddleware>()
				.UseMiddleware<MaintenanceMiddleware>()
				.UseMiddleware<VerifyPlayerTokenMiddleware>());

		return app;
	}
}
