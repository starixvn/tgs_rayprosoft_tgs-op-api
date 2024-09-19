using System.Text.Json;
using System.Text.Json.Nodes;
using Payment.GameWallet.Client;
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
					MerchantCode: GetValueOrThrow<string>(brandPaymentInfo, "merchantCode"),
					MerchantSecurityCode: GetValueOrThrow<string>(brandPaymentInfo, "securityCode"),
					CurrencyCode: GetValueOrThrow<string>(brandPaymentInfo, "currencyCode"),
					BankCode: GetValueOrThrow<string>(brandPaymentInfo, "bankCode"),
					Customer: requestInfo.PlayerCode,
					Reference: formId,
					SuccessUrl: "https://dictionary.cambridge.org/vi/dictionary/english/successful",
					FailUrl: "https://dictionary.cambridge.org/vi/dictionary/english/fail",
					Language: GetValueOrThrow<string>(brandPaymentInfo, "language"),
					ClientIP: requestInfo.PlayerIp,
					Method: GetValueOrThrow<string>(brandPaymentInfo, "method"),
					FeExpirationTime: brandPaymentInfo["feExpirationTime"]?.GetValue<double>(),
					BeExpirationTime: brandPaymentInfo["beExpirationTime"]?.GetValue<double>()),
				GameWalletUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	private static T GetValueOrThrow<T>(JsonNode jsonNode, string key)
		=> jsonNode[key] == null ? throw new ArgumentException($"Missing required field: {key}") : jsonNode[key]!.GetValue<T>();


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
