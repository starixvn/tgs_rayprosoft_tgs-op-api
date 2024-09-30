using System.Text.Json;
using Tgs.Payment.WFPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class WFPayCommandBuilder()
	: IDepositFormCommandBuilder
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
			$"tgs.payment.wfpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new WFPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("publicKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("paymentMethod").GetString()),
				WFPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<WFPayDepositApplyInfo>(
			applyInfo,
			WFPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			ThirdPartyOrderNo: info.Response.ThirdPartyOrderNo.ToString(),
			info.Response.RedirectUrl.ToString()));
	}
}