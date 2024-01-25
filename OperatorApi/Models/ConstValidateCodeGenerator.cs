using Microsoft.Extensions.Options;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Apis.OperatorApi.Models;

public class ConstValidateCodeGenerator(IValidateCodeGenerator validateCodeGenerator, IOptions<ConstValidateCodeOptions> optionsAccessor) : IValidateCodeGenerator
{
	private readonly ConstValidateCodeOptions _options = optionsAccessor.Value;

	public string GenerateCode()
		=> _options.ValidateCodeTestEnabled ? "123456" : validateCodeGenerator.GenerateCode();

	public string GenerateSecurityKey() => validateCodeGenerator.GenerateSecurityKey();
}
