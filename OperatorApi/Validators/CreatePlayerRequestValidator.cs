using FluentValidation;
using FluentValidation.Validators;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class CreatePlayerRequestValidator : AbstractValidator<CreatePlayerRequest>
{
    public CreatePlayerRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .Length(6, 32)
            .SetValidator(new RegularExpressionValidator<CreatePlayerRequest>("^[0-9a-zA-Z]+$"));

        RuleFor(x => x.Password)
            .NotEmpty()
            .Length(6, 20);
    }
}
