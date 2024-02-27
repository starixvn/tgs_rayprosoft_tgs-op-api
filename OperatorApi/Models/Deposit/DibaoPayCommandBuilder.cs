using System.Text.Json;
using Tgs.Payment.DibaoPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class DibaoPayCommandBuilder(IHttpContextAccessor httpContextAccessor) : IDepositFormCommandBuilder
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
			"tgs.payment.dibaopay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new DibaoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("channel").GetString(),
					httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString(),
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				DibaoPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<DibaoPayDepositApplyInfo>(
			applyInfo,
			DibaoPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.ThirdPartyOrderNo,
			info.Response.RedirectUrl.ToString()));
	}
}