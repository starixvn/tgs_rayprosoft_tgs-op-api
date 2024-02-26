using System.Text.Json;
using Tgs.Payment.Help2Pay;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class Help2PayCommandBuilder(
	IHttpContextAccessor httpContextAccessor,
	Help2PaySerializer serializer,
	IHelp2PayRepository redisRepository) : IDepositFormCommandBuilder
{
	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData!);

		var clientIp = httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.help2pay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new Help2PayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("securityCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currency").GetString(),
					brandPaymentInfo.RootElement.GetProperty("bankCode").GetString(),
					formId,
					"https://google.com",
					clientIp,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal())));

		return ValueTask.FromResult(command);
	}

	public async ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<Help2PayDepositApplyInfo>(applyInfo);

		if (string.IsNullOrWhiteSpace(info.Response.PaymentDepositId))
			throw new PaymentFailException("Something went wrong to deposit via help2pay");

		var identityId = Guid.NewGuid();

		_ = await redisRepository.StoreDepositHtmlAsync(identityId, info.Response.Html)
			.ConfigureAwait(false);

		return new DepositApplyResponse(
			info.Response.PaymentDepositId,
			$"/api/deposit/bank/help2pay/pay/{identityId}");
	}
}
