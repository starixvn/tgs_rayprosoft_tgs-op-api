using Microsoft.Extensions.Options;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Apis.OperatorApi.Models;

public class ConstValidateCodeGenerator : IValidateCodeGenerator
{
    private readonly ConstValidateCodeOptions _options;
    private readonly IValidateCodeGenerator _validateCodeGenerator;

    public ConstValidateCodeGenerator(IValidateCodeGenerator validateCodeGenerator, IOptions<ConstValidateCodeOptions> optionsAccessor)
    {
        _validateCodeGenerator = validateCodeGenerator;
        _options = optionsAccessor.Value;
    }

    public string GenerateCode()
    {
        if (_options.ValidateCodeTestEnabled)
            return "123456";
        else
            return _validateCodeGenerator.GenerateCode();
    }

    public string GenerateSecurityKey() => _validateCodeGenerator.GenerateSecurityKey();
}
