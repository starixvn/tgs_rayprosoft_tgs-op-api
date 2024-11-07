using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public class RegisterByPhoneRequestValidator : AbstractValidator<RegisterByPhoneRequest>
{
	public RegisterByPhoneRequestValidator()
	{
		_ = RuleFor(x => x.UserName)
		    .NotEmpty()
		    .Length(6, 32)
		    .Matches("^[0-9a-zA-Z]+$");

		_ = RuleFor(x => x.Password)
		    .NotEmpty()
		    .Length(6, 20);
	}
}