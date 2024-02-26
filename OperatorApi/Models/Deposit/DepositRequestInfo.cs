using Tgs.Payment.Clients;

namespace TGS.Apis.OperatorApi.Models.Deposit;

public record DepositRequestInfo(
	string PlayerCode,
	Guid BrandId,
	BrandPaymentInfo BrandPaymentInfo,
	decimal Amount,
	Guid? PromotionId);
