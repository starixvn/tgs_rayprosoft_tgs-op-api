using System.Text.Json;
using Tgs.Payment.HaloPay.Client;
using Tgs.PlayerService.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class HaloPayCommandBuilder(IPlayerServiceClient playerServiceClient) : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var playerPhone = GetPlayerPhoneAsync(requestInfo.PlayerCode).ToAsyncEnumerable().ToEnumerable().First();

		var hasPayType = brandPaymentInfo.RootElement.TryGetProperty("payType", out var payType);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			$"tgs.payment.halopay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new HaloPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					requestInfo.PlayerCode,
					hasPayType ? payType.GetString() : "",
					playerPhone,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				HaloPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<HaloPayDepositApplyInfo>(
			applyInfo,
			HaloPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}

	private async Task<string> GetPlayerPhoneAsync(string playerCode)
	{
		var playerProps = await playerServiceClient
			.GetPlayerPropertiesAsync(playerCode)
			.ConfigureAwait(false);

		return playerProps.GetValueOrDefault("Phone");
	}
}