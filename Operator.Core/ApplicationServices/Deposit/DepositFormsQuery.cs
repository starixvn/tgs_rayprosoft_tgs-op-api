using System;
using System.Collections.Generic;
using Tgs.Payment.Clients;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record DepositFormsQuery(
	Guid BrandId,
	string PlayerCode,
	IEnumerable<PaymentFlowState> Status,
	(DateTimeOffset Begin, DateTimeOffset End)? CreateOnRange,
	(DateTimeOffset Begin, DateTimeOffset End)? UpdateOnRange,
	Paging Paging);
