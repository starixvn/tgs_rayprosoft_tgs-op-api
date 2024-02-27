using FluentValidation;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
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
		_ = RuleFor(x => x.RealName)
			.NotNull()
			.NotEmpty();

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
			});
	}
}