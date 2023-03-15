using System;
using Tgs.Payment.Clients;

namespace TGS.Services.Operator;
public record DepositNotifyBody(
	Guid FormId,
	string? OrderNo,
	Guid BrandPaymentId,
	decimal Amount,
	PaymentFlowState Status,
	DateTime UpdatedOn);
