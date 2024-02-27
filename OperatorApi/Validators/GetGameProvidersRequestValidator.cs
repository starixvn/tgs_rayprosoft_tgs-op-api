using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGameProvidersRequestValidator : AbstractValidator<GetGameProvidersRequest>
{
	public GetGameProvidersRequestValidator()
	{
		_ = RuleFor(x => x.CurrencyCode)
			.NotEmpty();

		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty();

		_ = RuleFor(x => x.PlatformType)
			.IsInEnum()
			.WithMessage("value does not in enum");
	}
}