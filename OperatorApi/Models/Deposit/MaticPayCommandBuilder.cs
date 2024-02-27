using System.Text.Json;
using System.Web;
using Tgs.Payment.MaticPay;
using Tgs.PlayerService.Client;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class MaticPayCommandBuilder(
	IPlayerServiceClient playerServiceClient,
	MaticPaySerializer serializer)
	: IDepositFormCommandBuilder
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

		if (player is null || !player.IdentityAuthentication)
			throw new MaticPayDepositNeedRealNameException();

		var playerProperties = await playerServiceClient
			.GetPlayerPropertiesAsync(requestInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var realName = playerProperties.GetValueOrDefault("RealName");

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.maticpay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new MaticPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					requestInfo.Amount,
					realName)));

		return command;
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<MaticPayDepositApplyInfo>(applyInfo);

		if (info.Response.Code == "0")
		{
			var paymentUrl = info.Response.Detail.PayURL;
			var payUri = new Uri(paymentUrl ?? string.Empty);
			var thirdPartyOrderNo = HttpUtility.ParseQueryString(payUri.Query).Get("oid");

			return ValueTask.FromResult(new DepositApplyResponse(
				thirdPartyOrderNo,
				string.IsNullOrEmpty(paymentUrl)
					? info.Response.Detail.PayHtml ?? string.Empty
					: paymentUrl));
		}
		else
			throw new PaymentFailException(info.Response.Msg);
	}
}