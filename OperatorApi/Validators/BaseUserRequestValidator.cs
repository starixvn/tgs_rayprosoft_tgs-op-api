using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public abstract class BaseUserRequestValidator<T> : AbstractValidator<T> where T : BaseUserRequest
{
    protected BaseUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(6, 32)
            .WithName(x => x
                .WithPropertyExpression(o => o.Username)
                .GetJsonPropertyName(nameof(x.Username)));
    }
}
