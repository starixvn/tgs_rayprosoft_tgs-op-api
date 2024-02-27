namespace TGS.Services.Operator.ValidateCode;

public interface IValidateCodeGenerator
{
	string GenerateCode();

	string GenerateSecurityKey();
}