using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Mail;
using TGS.Services.Operator.Properties;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class StartSetPlayerEmailCommandService : IGetService<StartSetPlayerEmailCommand, string>
{
	private readonly IValidateCodeGenerator _codeGenerator;
	private readonly IEmailClient _emailClient;
	private readonly IValidateCodeOperator _validateCodeOperator;

	public StartSetPlayerEmailCommandService(
		IEmailClient emailClient,
		IValidateCodeGenerator codeGenerator,
		IValidateCodeOperator validateCodeOperator)
	{
		_emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
		_codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
		_validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
	}

	public async ValueTask<string> GetAsync(StartSetPlayerEmailCommand command, CancellationToken cancellationToken = default)
	{
		var generateCode = () => _codeGenerator.GenerateCode();
		if (command.IsRegisterFlow)
			generateCode = () => "123456";

		var saveResult = await Policy.HandleResult<(bool Result, string ValidateCode, string SecurityKey)>(data => !data.Result)
			.RetryAsync(5)
			.ExecuteAsync(async () =>
			{
				var validateCode = generateCode();
				var token = _codeGenerator.GenerateSecurityKey();

				var data = new EmailValidateData(command.Email, command.PlayerCode);

				var result = await _validateCodeOperator
					.SaveValidateDataAsync(validateCode, token, data)
					.ConfigureAwait(false);

				return (result, validateCode, token);
			})
			.ConfigureAwait(false);

		if (!saveResult.Result)
			throw new GenerateValidateCodeFailException();

		if (command.IsRegisterFlow)
		{
			return saveResult.SecurityKey;
		}

		var mailSubject = Resources.ResourceManager.GetString(
			nameof(Resources.ValidateCodeMailSubject),
			command.CultureInfo)!;
		var mailBody = string.Format(
			Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailBody), command.CultureInfo)!,
			saveResult.ValidateCode);

		var mailMessage = new SendEmailRequest(
			new[] { command.Email },
			mailSubject,
			mailBody);

		_ = await _emailClient.SendAsync(mailMessage).ConfigureAwait(false);

		return saveResult.SecurityKey;
	}
}
