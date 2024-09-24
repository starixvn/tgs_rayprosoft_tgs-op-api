using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Email.Client;
using TGS.Services.Operator.Properties;
using TGS.Services.ValidateCode.Clients;
using TGS.SmsService.Client;

namespace TGS.Services.Operator.ValidateCode;

internal class RestValidateCodeOperator(
	IValidateCodeServiceClient validateCodeServiceClient,
	ISmsServiceClient smsServiceClient,
	INotificationEmailClient notificationEmailClient
	)
	: IValidateCodeOperator
{
	public async ValueTask<TData?> GetValidateCodeDataAsync<TData>(string validateCode, string token)
	{
		var result = await validateCodeServiceClient
			.GetValidateCodeDataAsync(validateCode, token)
			.ConfigureAwait(false);

		return string.IsNullOrEmpty(result) ? default : DeserializeValidateCodeData<TData>(result);
	}

	public ValueTask<bool> SaveValidateDataAsync<TData>(string validateCode, string token, TData data)
	{
		return validateCodeServiceClient
			.SaveValidateCodeDataAsync(validateCode, token, SerializeValidateCodeData(data));
	}

	public async ValueTask SendEmailAsync(CultureInfo cultureInfo, string email, string validateCode)
	{
		var mailSubject = Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailSubject), cultureInfo)!;
		var mailBody = string.Format(
			Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailBody), cultureInfo)!,
			validateCode);

		await notificationEmailClient.PublishTextAsync(
			recipients: [email],
			subject: mailSubject,
			content: mailBody
		).ConfigureAwait(false);
	}

	public ValueTask SendMessageAsync(string phone, string shortMessage, string currency, Guid brandId)
		=> smsServiceClient.SendShortMessageAsync(phone, shortMessage, brandId);

	private static TData? DeserializeValidateCodeData<TData>(string json) => JsonSerializer.Deserialize<TData>(json);

	private static string SerializeValidateCodeData<TData>(TData data) => JsonSerializer.Serialize(data);
}