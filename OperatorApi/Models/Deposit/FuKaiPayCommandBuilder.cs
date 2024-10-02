using System.Text.Json;
using Tgs.Payment.FuKaiPay.Client;
using TGS.Apis.OperatorApi.Utils;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class FuKaiPayCommandBuilder(IHttpContextAccessor httpContextAccessor) : IDepositFormCommandBuilder
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
			$"tgs.payment.fukaipay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new FuKaiPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("merchantKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("channelCode").GetString(),
					requestInfo.PlayerCode,
					"PHP",
					IPUtils.GetRequestIP(httpContextAccessor.HttpContext)),
				FuKaiPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<FuKaiPayDepositApplyInfo>(
			applyInfo,
			FuKaiPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}