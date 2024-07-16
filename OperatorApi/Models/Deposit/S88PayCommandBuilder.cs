using System.Text.Json;
using Tgs.Payment.S88Pay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class S88PayCommandBuilder() : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			$"tgs.payment.s88pay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new S88PayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("secretKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("paymentCode").GetString(),
					requestInfo.PlayerCode,
					"VND",
					brandPaymentInfo.RootElement.GetProperty("bankCode").GetString()),
				S88PayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<S88PayDepositApplyInfo>(
			applyInfo,
			S88PayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}