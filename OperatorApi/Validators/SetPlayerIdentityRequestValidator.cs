using FluentValidation;
using Microsoft.AspNetCore.Http;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;
using TGS.IdentityVerifiers;
using TGS.Services.Operations;

namespace TGS.Apis.OperatorApi.Validators;

public class SetPlayerIdentityRequestValidator : AbstractValidator<SetPlayerIdentityRequest>
{
	public SetPlayerIdentityRequestValidator(
		IHttpContextAccessor httpContextAccessor,
		IPlayerOperations playerOperations,
		IIdentityVerifierSelector<string> identityVerifierSelector)
	{
		RuleFor(x => x.RealName)
			.NotNull()
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.RealName)
				.GetJsonPropertyName(nameof(x.RealName)));

		_ = RuleFor(x => x.Identity)
			.Must(id =>
			{
				var httpContext = httpContextAccessor.HttpContext;
				var playerTokenData = httpContext.Items.GetPlayerTokenData();

				var player = playerOperations
					.GetPlayerByCodeAsync(playerTokenData.PlayerCode)
					.GetAwaiter()
					.GetResult();

				var playerCurrency = player.DefaultWalletCurrencyCode;

				var identityVerifier = identityVerifierSelector.GetIdentityVerifier(playerCurrency);

				return identityVerifier.Verify(id);
			})
			.WithName(x => x
				.WithPropertyExpression(o => o.Identity)
				.GetJsonPropertyName(nameof(x.Identity)));
	}
}
