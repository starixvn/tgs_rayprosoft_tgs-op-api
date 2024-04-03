using System.Text.Json;
using Tgs.Payment.GCPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class GCPayCommandBuilder : IDepositFormCommandBuilder
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
			$"tgs.payment.{paymentCode.ToLower()}.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new GCPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("customerId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("payChannelId").GetString()),
				GCPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<GCPayDepositApplyInfo>(
			applyInfo,
			GCPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}