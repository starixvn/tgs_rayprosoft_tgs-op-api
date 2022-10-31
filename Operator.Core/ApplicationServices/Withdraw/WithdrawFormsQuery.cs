using System;
using System.Collections.Generic;
using Tgs.Payment.Clients;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record WithdrawFormsQuery(
	Guid BrandId,
	string PlayerCode,
	IEnumerable<PaymentFlowState> Status,
	(DateTimeOffset Begin, DateTimeOffset End)? CreateOnRange,
	(DateTimeOffset Begin, DateTimeOffset End)? UpdateOnRange,
	PageableRequest Paging);
