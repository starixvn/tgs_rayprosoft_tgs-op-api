using System.Text.Json;
using Tgs.Payment.UBPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class UBPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var canGetCurrency = brandPaymentInfo.RootElement.TryGetProperty("currency", out var jsonCurrency);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.ubpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new UBPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("notifyToken").GetString(),
					formId,
					canGetCurrency ? jsonCurrency.GetString() : null,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				UBPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(applyInfo, UBPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}
