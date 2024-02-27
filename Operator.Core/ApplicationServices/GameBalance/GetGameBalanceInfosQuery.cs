using System;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public record GetGameBalanceInfosQuery(
	Guid BrandId,
	string PlayerCode,
	(DateTimeOffset Begin, DateTimeOffset End)? RecordOnRange);