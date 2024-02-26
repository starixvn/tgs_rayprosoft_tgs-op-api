using System.Text.Json;
using Tgs.Payment.UmPay;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public class UmPayCommandBuilder(
	UmPaySerializer serializer,
	IGetService<GetUmPayExchangeRateQuery, string> getUmPayExchangeRateService)
	: IDepositFormCommandBuilder
{
	private static readonly Random _Random = new();

	public ValueTask<CreateDepositFormCommand> BuildDepositCommandAsync(
		string paymentCode,
		string bankType,
		DepositRequestInfo requestInfo,
		CancellationToken cancellationToken = default)
		=> bankType switch
		{
			_ when string.Equals("Crypto", bankType, StringComparison.InvariantCultureIgnoreCase)
				=> BuildCryptoDepositCommandAsync(requestInfo, cancellationToken),
			_ => BuildOtherDepositCommandAsync(requestInfo)
		};

	public ValueTask<DepositApplyResponse> ReadApplyInfoAsync(string applyInfo, CancellationToken cancellationToken = default)
	{
		var info = serializer.Deserialize<UmPayDepositApplyInfo>(applyInfo);

		return ValueTask.FromResult(info.Response.Code == "1"
			? new DepositApplyResponse(
				info.Response.TradeNo,
				info.Response.JumpUrl.ToString())
			: throw new PaymentFailException(info.Response.Msg));
	}

	private async ValueTask<CreateDepositFormCommand> BuildCryptoDepositCommandAsync(DepositRequestInfo requestInfo, CancellationToken cancellationToken)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		var query = new GetUmPayExchangeRateQuery(
			"tgs.payment.umpay.queryexchangerate",
			serializer.Serialize(
				new UmPayQueryExchangeRateSettings()));

		var response = await getUmPayExchangeRateService.GetAsync(
			query,
			cancellationToken).ConfigureAwait(false);

		var exchangeRateApplyInfo = serializer.Deserialize<UmPayQueryExchangeRateApplyInfo>(response);

		var rateResponse = exchangeRateApplyInfo.Response.Code == "1" ? exchangeRateApplyInfo.Response.Rate : throw new UmPayApiException();

		var currency = brandPaymentInfo.RootElement.GetProperty("currency").GetString();
		var usdt = rateResponse.RootElement.GetProperty("USDT");
		var rate = usdt.GetProperty(currency.ToUpper()).GetProperty("rate").GetDecimal();

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			requestInfo.Amount,
			"tgs.payment.umpay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new UmPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("tradeType").GetString(),
					"1",
					formId,
					requestInfo.Amount,
					rate,
					formId.ToString())));

		return command;
	}

	private ValueTask<CreateDepositFormCommand> BuildOtherDepositCommandAsync(DepositRequestInfo requestInfo)
	{
		var brandPaymentInfo = JsonDocument.Parse(requestInfo.BrandPaymentInfo.ExtendedData);

		//需要隨機兩位小數的通道類型:PAY01支轉支 PAY03轉數快 PAY04網銀
		var randomPointTradeTypes = new string[] { "PAY01", "PAY03", "PAY04" };

		var tradeType = brandPaymentInfo.RootElement.GetProperty("tradeType").GetString();
		var amount = requestInfo.Amount;
		if (randomPointTradeTypes.Contains(tradeType))
		{
			amount += _Random.Next(1, 100) / 100M;
		}

		var command = new CreateDepositFormCommand(
			requestInfo.PlayerCode,
			requestInfo.BrandId,
			requestInfo.BrandPaymentInfo.Id,
			amount,
			"tgs.payment.umpay.deposit",
			requestInfo.PromotionId,
			formId => serializer.Serialize(
				new UmPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					tradeType,
					"1",
					formId,
					amount,
					1m,
					formId.ToString())));

		return ValueTask.FromResult(command);
	}
}
