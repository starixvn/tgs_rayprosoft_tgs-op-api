using System.Text.Json;
using Tgs.Payment.LeePay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class LeePayCommandBuilder(LeePaySerializer serializer) : IDepositFormCommandBuilder
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
			"tgs.payment.leepay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new LeePayDepositSettings(
						brandPaymentInfo.RootElement.GetProperty("systemId").GetString(),
						brandPaymentInfo.RootElement.GetProperty("systemKey").GetString(),
						formId,
						brandPaymentInfo.RootElement.GetProperty("channelCurrency").GetString(),
						"http://localhost",
						brandPaymentInfo.RootElement.GetProperty("channel").GetString())));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<LeePayDepositApplyInfo>(applyInfo);

		return info.Response.Code != "00"
			? throw new PaymentFailException(info.Response.Message)
			: ValueTask.FromResult(new DepositApplyResponse(
				info.Response.OrderNo,
				info.Response.PaymentUrl));
	}
}
