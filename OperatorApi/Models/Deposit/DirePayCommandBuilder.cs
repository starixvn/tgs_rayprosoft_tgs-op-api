using System.Text.Json;
using Tgs.Payment.DirePay.Client;
using Tgs.PlayerService.Client;
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
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

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
					brandPaymentInfo.RootElement.GetProperty("email").GetString(),
					brandPaymentInfo.RootElement.GetProperty("password").GetString(),
					brandPaymentInfo.RootElement.GetProperty("secret").GetString(),
					formId,
					new Uri("https://localhost"),
					"VND",
					brandPaymentInfo.RootElement.GetProperty("method").GetString(),
					requestInfo.PlayerCode,
					playerRealName,
					brandPaymentInfo.RootElement.GetProperty("bankCode").GetString(),
					PhoneNumber: null,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
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