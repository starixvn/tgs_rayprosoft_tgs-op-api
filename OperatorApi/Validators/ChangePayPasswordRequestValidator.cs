using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class ChangePayPasswordRequestValidator : AbstractValidator<ChangePayPasswordRequest>
{
	public ChangePayPasswordRequestValidator()
	{
		_ = RuleFor(x => x.NewPassword)
			.NotEmpty()
			.Length(6, 20);
	}
}
