using System.Text.Json;
using Tgs.Payment.SkyPay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class SkyPayCommandBuilder(SkyPaySerializer serializer) : IDepositFormCommandBuilder
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
			$"tgs.payment.{paymentCode}.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new SkyPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("accessToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("gateway").GetString(),
					brandPaymentInfo.RootElement.GetProperty("transfer").GetString(),
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal(),
					formId)));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<SkyPayDepositApplyInfo>(applyInfo);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.TradeNo,
			info.Response.TradeUrl));
	}
}