using System;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteGameRecordViewModel
{
	public DateTimeOffset CreatedOn { get; init; }

	public DateOnly Date { get; init; }

	public Guid Id { get; init; }

	public int ItemIndex { get; init; }
}
