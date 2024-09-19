using System.Text.Json;
using Payment.SpeedPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class SpeedPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(string paymentCode, string bankType, DepositRequestInfo requestInfo, CancellationToken cancellationToken = default)
	{
		var speedPaySetting = JsonSerializer.Deserialize<SpeedPayDepositSettings>(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.speedpay.deposit",
			requestInfo.PromotionId,
			formId =>
			{
				speedPaySetting.OrderNo = formId;
				return JsonSerializer.Serialize(speedPaySetting, SpeedPayUtility.SerializerOptions);
			});

		return ValueTask.FromResult(command);
	}
	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<SpeedPayDepositApplyInfo>(
			applyInfo,
			SpeedPayUtility.SerializerOptions);

		var response = new DepositApplyResponse(info.Response.ThirdPartyOrderNo, info.Response.RedirectUrl.ToString());

		return ValueTask.FromResult(response);
	}
}
