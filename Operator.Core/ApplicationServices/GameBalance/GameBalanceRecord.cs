using System;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public class GameBalanceRecord
{
	public decimal Amount { get; init; }

	public DateTimeOffset BalanceTime { get; init; }

	public string From { get; init; } = default!;

	public string To { get; init; } = default!;
}
