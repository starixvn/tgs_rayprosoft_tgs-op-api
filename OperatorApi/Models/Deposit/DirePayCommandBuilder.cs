using System.Text.Json;
using System.Text.Json.Nodes;
using Tgs.Payment.DirePay.Client;
using Tgs.PlayerService.Client;
using TGS.Apis.OperatorApi.Utils;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class DirePayCommandBuilder(IPlayerServiceClient playerServiceClient)
	: IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonNode.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var playerRealName = GetPlayerRealNameAsync(requestInfo.PlayerCode).ToAsyncEnumerable().ToEnumerable().First();

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			$"tgs.payment.direpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new DirePayDepositSettings(
					brandPaymentInfo.GetValueOrThrow<string>("email"),
					brandPaymentInfo.GetValueOrThrow<string>("password"),
					brandPaymentInfo.GetValueOrThrow<string>("secret"),
					formId,
					brandPaymentInfo.GetValueOrDefault("redirectUrl", new Uri("https://www.google.com/search?q=success")),
					brandPaymentInfo.GetValueOrThrow<string>("currencyCode"),
					brandPaymentInfo.GetValueOrThrow<string>("method"),
					requestInfo.PlayerCode,
					string.IsNullOrEmpty(playerRealName) ? "UserTest" : playerRealName,
					brandPaymentInfo.GetValueOrThrow<string>("bankCode"),
					PhoneNumber: null,
					brandPaymentInfo.GetValueOrThrow<decimal>("exchangeRate")),
				DirePayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<DirePayDepositApplyInfo>(
			applyInfo,
			DirePayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			thirdPartyOrderNo: info.Response.FormId.ToString(),
			info.Response.RedirectUrl.ToString()));
	}

	private async Task<string> GetPlayerRealNameAsync(string playerCode)
	{
		var playerProps = await playerServiceClient
			.GetPlayerPropertiesAsync(playerCode)
			.ConfigureAwait(false);

		return playerProps.GetValueOrDefault("RealName");
	}
}