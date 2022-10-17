using System;
using System.Linq;
using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGamesRequestValidator : AbstractValidator<GetGamesRequest>
{
    public GetGamesRequestValidator()
    {
		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.LanguageCode)
				.GetBindPropertyName(nameof(x.LanguageCode)));

		_ = RuleFor(x => x.CurrencyCode)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.CurrencyCode)
				.GetBindPropertyName(nameof(x.CurrencyCode)));

		_ = RuleFor(x => x.PlatformType)
			.IsInEnum()
			.WithMessage(x => string.Format(
				"{0} does not exist",
				x.WithPropertyExpression(o => o.PlatformType)
					.GetBindPropertyName(nameof(x.PlatformType))));

		_ = RuleFor(x => x.Types)
			.Must(ValidateGameTypes)
			.When(x => x.Types != null && x.Types.Length > 0)
			.WithMessage(x => string.Format(
				"one of the {0} does not exist",
				x.WithPropertyExpression(o => o.Types)
					.GetBindPropertyName(nameof(x.Types))));
    }

    private static bool ValidateGameTypes(GameType[] gameTypes) =>
        gameTypes.All(type => Enum.IsDefined(typeof(GameType), type));
}
