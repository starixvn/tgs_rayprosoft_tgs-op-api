using System.Text.Json;
using Tgs.Payment.AntPay.Client;
using Tgs.PlayerService.Client;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class AntPayCommandBuilder(
	IPlayerServiceClient playerServiceClient,
	IHttpContextAccessor httpContextAccessor)
	: IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var HasChannelCode = brandPaymentInfo.RootElement.TryGetProperty("channelCode", out var channelCode);

		var playerRealName = GetPlayerRealNameAsync(requestInfo.PlayerCode).ToAsyncEnumerable().ToEnumerable().First();

		var deviceType = httpContextAccessor.HttpContext.Items.GetPlayerTokenData().PlatformType == Core.Models.Enums.PlatformType.Desktop
			? "PC"
			: "MOBILE";

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			$"tgs.payment.antpay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new AntPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("source").GetInt32(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetInt32(),
					HasChannelCode ? channelCode.GetString() : null,
					requestInfo.PlayerCode,
					playerRealName,
					httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString(),
					deviceType),
				AntPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<AntPayDepositApplyInfo>(
			applyInfo,
			AntPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			ThirdPartyOrderNo: info.Response.FormId.ToString(),
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