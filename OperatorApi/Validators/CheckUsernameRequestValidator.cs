using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class CheckUsernameRequestValidator : AbstractValidator<CheckUsernameRequest>
{
	public CheckUsernameRequestValidator()
	{
		_ = RuleFor(x => x.Username)
			.NotEmpty()
			.Length(6, 32);
	}
}
