using System.Text.Json;
using Payment.BigPayz.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class BigPayzCommandBuilder : IDepositFormCommandBuilder
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
			"tgs.payment.bigpayz.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new BigPayzDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currencyCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					requestInfo.PlayerCode,
					$"Deposit_{formId}"),
				BigPayzUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<BigPayzDepositApplyInfo>(
			applyInfo,
			BigPayzUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.Transaction,
			info.Response.RedirectUrl.ToString()));
	}
}