using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class CheckUsernameRequestValidator : AbstractValidator<CheckUsernameRequest>
{
    public CheckUsernameRequestValidator()
    {
		_ = RuleFor(x => x.Username)
			.NotEmpty()
			.Length(6, 32)
			.WithName(x => x
				.WithPropertyExpression(o => o.Username)
				.GetBindPropertyName(nameof(x.Username)));
    }
}
