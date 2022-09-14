using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;

namespace TGS.Apis.OperatorApi.Validators;

public sealed class GetGameUrlRequestValidator : AbstractValidator<GetGameUrlRequest>
{
    public GetGameUrlRequestValidator()
    {
        RuleFor(x => x.SubGameProviderCode)
            .NotEmpty()
            .WithName(x => x
                .WithPropertyExpression(o => o.SubGameProviderCode)
                .GetBindPropertyName(nameof(x.SubGameProviderCode)));

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .WithName(x => x
                .WithPropertyExpression(o => o.LanguageCode)
                .GetBindPropertyName(nameof(x.LanguageCode)));
    }
}
