using System;

namespace TGS.Services.Operator.BrandPayment;

public record BrandPaymentVip(
	Guid BrandPaymentId,
	Guid VipLevelId);
