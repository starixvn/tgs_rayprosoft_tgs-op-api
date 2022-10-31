using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class LoginRequestValidators : BaseUserRequestValidator<LoginRequest>
{
	public LoginRequestValidators()
	{
		_ = RuleFor(x => x.Password)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.Password)
				.GetJsonPropertyName(x.Password));

		_ = RuleFor(x => x.PlatformType)
			.IsInEnum()
			.WithMessage(x => string.Format(
				"{0} does not exist",
				x.WithPropertyExpression(o => o.PlatformType)
					.GetJsonPropertyName(nameof(x.PlatformType))));
	}
}
