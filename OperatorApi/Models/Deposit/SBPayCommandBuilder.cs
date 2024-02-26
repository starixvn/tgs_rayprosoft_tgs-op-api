using System.Text.Json;
using Tgs.Payment.SBPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class SBPayCommandBuilder : IDepositFormCommandBuilder
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
			"tgs.payment.sbpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new SBPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("notifyToken").GetString(),
					formId,
					null,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				SBPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<SBPayDepositApplyInfo>(
			applyInfo,
			SBPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}
