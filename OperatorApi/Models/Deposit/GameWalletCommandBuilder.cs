using System.Text.Json;
using Payment.GameWallet.Client;
using Tgs.Payment.U2Pay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class GameWalletCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(string paymentCode, string bankType, DepositRequestInfo requestInfo, CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.gamewallet.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new GameWalletDepositSettings(
					MerchantCode: brandPaymentInfo.RootElement.GetProperty("merchantCode").GetString(),
					MerchantSecurityCode: brandPaymentInfo.RootElement.GetProperty("securityCode").GetString(),
					CurrencyCode: brandPaymentInfo.RootElement.GetProperty("currencyCode").GetString(),
					BankCode: brandPaymentInfo.RootElement.GetProperty("bankCode").GetString(),
					Customer: requestInfo.PlayerCode,
					Reference: formId,
					SuccessUrl: "https://dictionary.cambridge.org/vi/dictionary/english/successful",
					FailUrl: "https://dictionary.cambridge.org/vi/dictionary/english/fail",
					Language: brandPaymentInfo.RootElement.GetProperty("language").GetString(),
					ClientIP: brandPaymentInfo.RootElement.GetProperty("PlayerIp").GetString(),
					Method: brandPaymentInfo.RootElement.GetProperty("method").GetString()),
				GameWalletUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}
	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<U2PayDepositApplyInfo>(
			applyInfo,
			U2PayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}
