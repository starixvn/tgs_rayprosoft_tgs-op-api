using System.Text.Json;
using System.Text.Json.Nodes;
using Payment.GameWallet.Client;
using TGS.Apis.OperatorApi.Utils;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class GameWalletCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(string paymentCode, string bankType, DepositRequestInfo requestInfo, CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonNode.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.gamewallet.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new GameWalletDepositSettings(
					MerchantCode: brandPaymentInfo.GetValueOrThrow<string>("merchantCode"),
					MerchantSecurityCode: brandPaymentInfo.GetValueOrThrow<string>("securityCode"),
					CurrencyCode: brandPaymentInfo.GetValueOrThrow<string>("currencyCode"),
					BankCode: brandPaymentInfo.GetValueOrThrow<string>("bankCode"),
					Customer: requestInfo.PlayerCode,
					Reference: formId,
					SuccessUrl: brandPaymentInfo.GetValueOrDefault("successUrl", "https://www.google.com/search?q=success"),
					FailUrl: brandPaymentInfo.GetValueOrDefault("failUrl", "https://www.google.com/search?q=fail"),
					Language: brandPaymentInfo.GetValueOrThrow<string>("language"),
					ClientIP: requestInfo.PlayerIp,
					Method: brandPaymentInfo.GetValueOrThrow<string>("method"),
					FeExpirationTime: brandPaymentInfo["feExpirationTime"]?.GetValue<double>(),
					BeExpirationTime: brandPaymentInfo["beExpirationTime"]?.GetValue<double>()),
				GameWalletUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<GameWalletDepositApplyInfo>(
			applyInfo,
			GameWalletUtility.SerializerOptions);
		var response = new DepositApplyResponse(
			"",
			info.Response.RedirectUrl.ToString())
		{
			ExpirationTime = info.Settings.FeExpirationTime,
		};

		return ValueTask.FromResult(response);
	}
}
