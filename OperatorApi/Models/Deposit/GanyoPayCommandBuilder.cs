using System.Text.Json;
using Tgs.Payment.GanyoPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class GanyoPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.ganyopay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new GanyoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("customerId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					formId,
					CustomData: requestInfo.PlayerCode,
					PhoneNumber: null,
					brandPaymentInfo.RootElement.GetProperty("channel").GetString()),
				GanyoPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<GanyoPayDepositApplyInfo>(
			applyInfo,
			GanyoPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}
