using System.Text.Json;
using Payment.GameWallet.Client;
using Payment.SpeedPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class SpeedPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(string paymentCode, string bankType, DepositRequestInfo requestInfo, CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.speedpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new SpeedPayDepositSettings
				{
					MemberCode = brandPaymentInfo.RootElement.GetProperty("memberCode").GetString(),
					BankCode = brandPaymentInfo.RootElement.GetProperty("bankCode").GetString(),
					Key = brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					MerchantNo = brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					OrderNo = formId,
					PaymentCode = brandPaymentInfo.RootElement.GetProperty("paymentCode").GetString(),
					ReturnUrl = brandPaymentInfo.RootElement.GetProperty("merchantCode").GetString(),
					Version = brandPaymentInfo.RootElement.GetProperty("version").GetString()

				},
				GameWalletUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}
	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<SpeedPayDepositApplyInfo>(
			applyInfo,
			SpeedPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.FormId.ToString(),
			info.Response.RedirectUrl.ToString()));
	}
}
