using System;

namespace TGS.Services.Operator.ApplicationServices.BrandPayments;

public record BrandPaymentsQuery(
	Guid BrandId,
	string PlayerCode);
