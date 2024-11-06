using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Tgs.CashService.Client;
using TGS.Services.Operations;
using TGS.Services.Operator.Properties;
using Tgs.ServiceUtility;
using TGS.SmsService.Client;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Sms;

internal class SendOtpCommandService(
	ISmsServiceClient smsServiceClient, 
	IValidateCodeGenerator codeGenerator,
	IValidateCodeOperator validateCodeOperator) : ICommandService<SendOtpCommand>
{
	public async ValueTask ExecuteAsync(SendOtpCommand command, CancellationToken cancellationToken = default)
	{
		var saveResult = await Policy.HandleResult<(bool Result, string ValidateCode, string Token)>(data => !data.Result)
			.RetryAsync(5)
			.ExecuteAsync(async () =>
			{
				var validateCode = codeGenerator.GenerateCode();
				var token = codeGenerator.GenerateSecurityKey();

				var data = new ValidateCodeData($"{command.BrandId}-{command.AreaCode}-{command.Phone}", token);

				var result = await validateCodeOperator
					.SaveValidateDataAsync(validateCode, token, data)
					.ConfigureAwait(false);

				return (result, validateCode, token);
			})
			.ConfigureAwait(false);

		if (!saveResult.Result)
			throw new GenerateValidateCodeFailException();

		await smsServiceClient.SendShortMessageAsync(
			$"{command.AreaCode}{command.Phone}",
			string.Format(
				Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailBody), command.CultureInfo)!,
				saveResult.ValidateCode),
			command.BrandId,
			areaCode: command.AreaCode,
			phone: command.Phone,
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
}