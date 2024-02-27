using System.Text.Json;
using Tgs.Payment.TuPay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class TuPayCommandBuilder(TuPaySerializer serializer) : IDepositFormCommandBuilder
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
			"tgs.payment.tupay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new TuPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchant").GetString(),
					brandPaymentInfo.RootElement.GetProperty("secret").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId)));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<TuPayDepositApplyInfo>(applyInfo);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.TradeNo,
			info.Response.RedirectUrl.ToString()));
	}
}