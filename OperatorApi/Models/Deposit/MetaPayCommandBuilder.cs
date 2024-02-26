using System.Text.Json;
using Tgs.Payment.MetaPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class MetaPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData!);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.metapay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new MetaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("appId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("publicKey").GetString(),
					MetaPayDepositType.Parse(brandPaymentInfo.RootElement.GetProperty("payType").GetString()),
					new Uri("http://localhost"),
					formId,
					Phone: "",
					RealName: ""),
				MetaPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<MetaPayDepositApplyInfo>(
			applyInfo,
			MetaPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}
