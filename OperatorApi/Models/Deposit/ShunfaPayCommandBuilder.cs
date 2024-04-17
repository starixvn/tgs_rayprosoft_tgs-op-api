using System.Text.Json;
using Tgs.Payment.ShunfaPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class ShunfaPayCommandBuilder : IDepositFormCommandBuilder
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
			$"tgs.payment.shunfapay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new ShunfaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("clientId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("type").GetString()),
				ShunfaPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<ShunfaPayDepositApplyInfo>(
			applyInfo,
			ShunfaPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}