using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class AuthRequestValidator : AbstractValidator<AuthRequest>
{
    public AuthRequestValidator()
    {
		_ = RuleFor(x => x.ApiKey)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.ApiKey)
				.GetJsonPropertyName(nameof(x.ApiKey)));

		_ = RuleFor(x => x.Currency)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.Currency)
				.GetJsonPropertyName(nameof(x.Currency)));
    }
}
