using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGameUrlRequestValidator : AbstractValidator<GetGameUrlRequest>
{
	public GetGameUrlRequestValidator()
	{
		_ = RuleFor(x => x.GameProviderCode)
			.NotEmpty();

		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty();
	}
}