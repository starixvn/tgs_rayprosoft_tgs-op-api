using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGameProvidersRequestValidator : AbstractValidator<GetGameProvidersRequest>
{
    public GetGameProvidersRequestValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .WithName(x => x
                .WithPropertyExpression(o => o.CurrencyCode)
                .GetBindPropertyName(nameof(x.CurrencyCode)));

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .WithName(x => x
                .WithPropertyExpression(o => o.LanguageCode)
                .GetBindPropertyName(nameof(x.LanguageCode)));

        RuleFor(x => x.PlatformType)
            .IsInEnum()
            .WithMessage(x => string.Format(
                "{0} does not exist",
                x.WithPropertyExpression(o => o.PlatformType)
                    .GetBindPropertyName(nameof(x.PlatformType))));
    }
}
