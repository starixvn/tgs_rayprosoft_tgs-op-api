using System.Collections.Generic;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator;

public class RoundsResponse : MessageResponse
{
    public IEnumerable<RoundModel> Items { get; init; } = default!;

    public RoundTotalModel Total { get; init; } = default!;

    public int TotalCount { get; init; }
}