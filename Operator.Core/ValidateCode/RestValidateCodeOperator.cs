using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Mail;
using TGS.Services.Operator.Properties;
using TGS.Services.ValidateCode.Clients;
using TGS.SmsService.Client;

namespace TGS.Services.Operator.ValidateCode;

internal class RestValidateCodeOperator(
	IValidateCodeServiceClient validateCodeServiceClient,
	ISmsServiceClient smsServiceClient,
	IEmailClient emailClient) : IValidateCodeOperator
{
	private readonly IEmailClient _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
	private readonly ISmsServiceClient _smsServiceClient = smsServiceClient ?? throw new ArgumentNullException(nameof(smsServiceClient));
	private readonly IValidateCodeServiceClient _validateCodeServiceClient = validateCodeServiceClient ?? throw new ArgumentNullException(nameof(validateCodeServiceClient));

	public async ValueTask<TData?> GetValidateCodeDataAsync<TData>(string validateCode, string token)
	{
		var result = await _validateCodeServiceClient
			.GetValidateCodeDataAsync(validateCode, token)
			.ConfigureAwait(false);

		return string.IsNullOrEmpty(result) ? default : DeserializeValidateCodeData<TData>(result);
	}

	public ValueTask<bool> SaveValidateDataAsync<TData>(string validateCode, string token, TData data)
	{
		return _validateCodeServiceClient
			.SaveValidateCodeDataAsync(validateCode, token, SerializeValidateCodeData(data));
	}

	public async ValueTask SendEmailAsync(CultureInfo cultureInfo, string email, string validateCode)
	{
		var mailSubject = Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailSubject), cultureInfo)!;
		var mailBody = string.Format(
			Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailBody), cultureInfo)!,
			validateCode);

		var mailMessage = new SendEmailRequest(
			[email],
			mailSubject,
			mailBody);

		_ = await _emailClient.SendAsync(mailMessage).ConfigureAwait(false);
	}

	public ValueTask SendMessageAsync(string phone, string shortMessage, string currency, Guid brandId)
	{
		return _smsServiceClient.SendShortMessageAsync(phone, shortMessage, brandId);
	}

	private static TData? DeserializeValidateCodeData<TData>(string json) => JsonSerializer.Deserialize<TData>(json);

	private static string SerializeValidateCodeData<TData>(TData data) => JsonSerializer.Serialize(data);
}