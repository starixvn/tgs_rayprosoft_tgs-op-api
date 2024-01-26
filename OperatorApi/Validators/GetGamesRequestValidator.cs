using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGamesRequestValidator : AbstractValidator<GetGamesRequest>
{
	public GetGamesRequestValidator()
	{
		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty();

		_ = RuleFor(x => x.CurrencyCode)
			.NotEmpty();

		_ = RuleFor(x => x.PlatformType)
			.IsInEnum()
			.WithMessage("value does not in enum.");

		_ = RuleFor(x => x.Types)
			.Must(ValidateGameTypes)
			.When(x => x.Types != null && x.Types.Length > 0)
			.WithMessage("GameType must in enum.");
	}

	private static bool ValidateGameTypes(GameType[] gameTypes) =>
		gameTypes.All(type => Enum.IsDefined(typeof(GameType), type));
}
