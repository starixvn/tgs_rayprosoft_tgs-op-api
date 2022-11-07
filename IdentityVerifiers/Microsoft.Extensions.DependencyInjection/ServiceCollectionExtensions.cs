using TGS.IdentityVerifiers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddIdentityVerifiers(this IServiceCollection services)
		=> services
			.AddTransient<IIdentityVerifierSelector<string>, CurrencyIdentityVerifierSelector>();
}
