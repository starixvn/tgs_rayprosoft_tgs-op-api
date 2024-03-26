using System.Text.Json;
using Tgs.Payment.U2Pay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class U2PayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var hasBankName = brandPaymentInfo.RootElement.TryGetProperty("bankName", out var bankName);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.u2pay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new U2PayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("appSecret").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("channelNo").GetInt32(),
					hasBankName ? bankName.GetString() : "",
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				U2PayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<U2PayDepositApplyInfo>(
			applyInfo,
			U2PayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}