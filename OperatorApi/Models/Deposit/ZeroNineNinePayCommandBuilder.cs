using System.Text.Json;
using Tgs.Payment.ZeroNineNinePay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class ZeroNineNinePayCommandBuilder(ZeroNineNinePaySerializer serializer) : IDepositFormCommandBuilder
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
			"tgs.payment.zeronineninepay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new ZeroNineNinePayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetInt32(),
					brandPaymentInfo.RootElement.GetProperty("merchantKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId,
					"127.0.0.1",
					null)));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<ZeroNineNinePayDepositApplyInfo>(applyInfo);

		return ValueTask.FromResult(info.Response.Code == 200
			? new DepositApplyResponse(
				info.Response.Data.TradeNo,
				info.Response.Data.PayUrl)
			: throw new PaymentFailException(info.Response.Message));
	}
}