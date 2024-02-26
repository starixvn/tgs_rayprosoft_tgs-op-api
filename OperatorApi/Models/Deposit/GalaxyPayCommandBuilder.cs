using System.Text.Json;
using Tgs.Payment.PhpGoPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class GalaxyPayCommandBuilder : IDepositFormCommandBuilder
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
			$"tgs.payment.{paymentCode.ToLower()}.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new PhpGoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchant").GetString(),
					brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					PhpGoPaymentType.Parse(
						brandPaymentInfo.RootElement.GetProperty("paymentType").GetString()),
					PhpGoBankCode.Parse(
						brandPaymentInfo.RootElement.GetProperty("bankCode").GetString()),
					new Uri("http://localhost"),
					formId)));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(
			applyInfo,
			PhpGoPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			info.Response.FormId.ToString(),
			info.Response.RedirectUrl.ToString()));
	}
}
