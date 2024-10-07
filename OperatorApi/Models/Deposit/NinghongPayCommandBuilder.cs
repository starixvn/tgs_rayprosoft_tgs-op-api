using System.Text.Json;
using Tgs.Payment.NinghongPay.Client;
using Tgs.PlayerService.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class NinghongPayCommandBuilder(IPlayerServiceClient playerServiceClient) 
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
			$"tgs.payment.ninghongpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new NinghongPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("secretKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("method").GetInt32(),
					playerRealName),
				NinghongPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<NinghongPayDepositApplyInfo>(
			applyInfo,
			NinghongPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			ThirdPartyOrderNo: info.Response.ThirdPartyOrderNo.ToString(),
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