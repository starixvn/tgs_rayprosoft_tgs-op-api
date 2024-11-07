using System.Threading;
using System.Threading.Tasks;
using TGS.Services.Operator.ValidateCode;
using TGS.Services.ValidateCode.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Sms;

public class ValidateOtpCommandService(IValidateCodeServiceClient validateCodeOperator) : ICommandService<ValidateOtpCommand>
{
	public async ValueTask ExecuteAsync(ValidateOtpCommand command, CancellationToken cancellationToken = new CancellationToken())
	{
		var result = await validateCodeOperator.GetValidateCodeDataAsync(command.ValidateCode, command.SecurityKey).ConfigureAwait(false);

		if (string.IsNullOrEmpty(result))
			throw new ValidateCodeNotVerifyException();
	}
}