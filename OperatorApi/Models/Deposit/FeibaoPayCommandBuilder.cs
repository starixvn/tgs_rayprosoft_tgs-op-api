using System.Text.Json;
using Tgs.Payment.FeibaoPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class FeibaoPayCommandBuilder : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var hasBankCode = brandPaymentInfo.RootElement.TryGetProperty("bankCode", out var jsonBankCode);

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			$"tgs.payment.feibaopay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new FeibaoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchant").GetString(),
					brandPaymentInfo.RootElement.GetProperty("hashKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("hashIV").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("gateWay").GetString(),
					hasBankCode ? jsonBankCode.GetString() : null,
					requestInfo.PlayerCode,
					"127.0.0.1"),
				FeibaoPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<FeibaoPayDepositApplyInfo>(
			applyInfo,
			FeibaoPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}