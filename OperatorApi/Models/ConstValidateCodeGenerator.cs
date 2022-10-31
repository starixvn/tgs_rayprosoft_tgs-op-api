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
		=> _options.ValidateCodeTestEnabled ? "123456" : _validateCodeGenerator.GenerateCode();

	public string GenerateSecurityKey() => _validateCodeGenerator.GenerateSecurityKey();
}
