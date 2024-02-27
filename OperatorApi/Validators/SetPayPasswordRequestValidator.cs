using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class SetPayPasswordRequestValidator : AbstractValidator<SetPayPasswordRequest>
{
	public SetPayPasswordRequestValidator()
	{
		_ = RuleFor(x => x.Password)
			.NotEmpty()
			.Length(6, 20);
	}
}