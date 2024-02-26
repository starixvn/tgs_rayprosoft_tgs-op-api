using System.Text.Json;
using Tgs.Payment.PrimePay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class PrimePayCommandBuilder(PrimePaySerializer serializer) : IDepositFormCommandBuilder
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
			"tgs.payment.primepay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new PrimePayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					brandPaymentInfo.RootElement.GetProperty("iv").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currency").GetString(),
					formId,
					"https://google.com",
					brandPaymentInfo.RootElement.TryGetProperty("paymentMethod", out var prop)
						? prop.GetString()
						: null)));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<PrimePayDepositApplyInfo>(applyInfo);

		if (info.Response is PrimePayDepositSuccessResponse response)
		{
			return ValueTask.FromResult(new DepositApplyResponse(
				response.ReferenceId,
				response.RedirectUrl.ToString()));
		}
		else
		{
			var failResponse = (PrimePayDepositFailResponse)info.Response;

			throw new PaymentFailException(failResponse.Errors);
		}
	}
}
