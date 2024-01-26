using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class AuthRequestValidator : AbstractValidator<AuthRequest>
{
	public AuthRequestValidator()
	{
		_ = RuleFor(x => x.ApiKey)
			.NotEmpty();

		_ = RuleFor(x => x.Currency)
			.NotEmpty();
	}
}
