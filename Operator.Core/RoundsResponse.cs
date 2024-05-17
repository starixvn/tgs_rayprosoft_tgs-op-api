using TGS.Core.Models.Messages;

namespace TGS.Services.Operator;

public class RoundsResponse : MessageResponse
{
    public RoundModel[] Items { get; init; } = default!;

    public RoundTotalModel Total { get; init; } = default!;

    public int TotalCount { get; init; }
}