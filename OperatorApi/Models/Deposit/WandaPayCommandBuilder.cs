using System.Text.Json;
using Tgs.Payment.WandaPay.Client;
using Tgs.PlayerService.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class WandaPayCommandBuilder(IPlayerServiceClient playerServiceClient) : IDepositFormCommandBuilder
{
	public async ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var player = await playerServiceClient
		.GetPlayerInfoAsync(requestInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var playerProperties = await playerServiceClient
			.GetPlayerPropertiesAsync(requestInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var realName = playerProperties.GetValueOrDefault("RealName");

		if (string.IsNullOrWhiteSpace(realName))
			throw new WandaPayNeedChineseRealNameException();

		if (IsChinese(realName) == false)
			throw new WandaPayNeedChineseRealNameException();

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.wandapay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new WandaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("storeCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("signKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId,
					realName)));

		return command;
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<WandaPayDepositApplyInfo>(applyInfo);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.OrderNo,
			info.Response.PayUrl));
	}

	private static bool IsChinese(string strRealName)
	{
		var flag = true;
		int dstringmax = Convert.ToInt32("9fff", 16);
		int dstringmin = Convert.ToInt32("4e00", 16);
		for (var i = 0; i < strRealName.Length; i++)
		{
			var dRange = Convert.ToInt32(Convert.ToChar(strRealName.Substring(i, 1)));
			if (dRange >= dstringmin && dRange < dstringmax)
			{
				flag = true;
			}
			else
			{
				flag = false;
				break;
			}
		}

		return flag;
	}
}
