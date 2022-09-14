using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class ChangePayPasswordRequestValidator : AbstractValidator<ChangePayPasswordRequest>
{
    public ChangePayPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Length(6, 20)
            .WithName(x => x
                .WithPropertyExpression(o => o.NewPassword)
                .GetBindPropertyName(nameof(x.NewPassword)));
    }
}
