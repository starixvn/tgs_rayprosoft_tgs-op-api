using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class LoginRequestValidators : BaseUserRequestValidator<LoginRequest>
{
	public LoginRequestValidators()
	{
		_ = RuleFor(x => x.Password)
			.NotEmpty();

		_ = RuleFor(x => x.PlatformType)
			.IsInEnum();
	}
}
