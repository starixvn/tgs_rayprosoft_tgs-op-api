using System.Text.Json;
using Tgs.Payment.ApolloPay.Client;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class ApolloPayCommandBuilder() : IDepositFormCommandBuilder
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
			$"tgs.payment.apollopay.deposit",
			requestInfo.PromotionId,
			formId => JsonSerializer.Serialize(
				new ApolloPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("channelType").GetString(),
					hasBankCode ? jsonBankCode.GetString() : null,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal()),
				ApolloPayUtility.SerializerOptions));

		return ValueTask.FromResult(command);
	}

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = JsonSerializer.Deserialize<ApolloPayDepositApplyInfo>(
			applyInfo,
			ApolloPayUtility.SerializerOptions);

		return ValueTask.FromResult(new DepositApplyResponse(
			ThirdPartyOrderNo: info.Response.ThirdPartyOrderNo.ToString(),
			info.Response.RedirectUrl.ToString()));
	}
}