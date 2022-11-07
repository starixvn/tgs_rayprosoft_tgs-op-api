using System;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record CreateDepositFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid BrandPaymentId,
	decimal Amount,
	string Subject,
	Guid? PromotionId,
	Func<Guid, string?> CreatePaymentSettings);
