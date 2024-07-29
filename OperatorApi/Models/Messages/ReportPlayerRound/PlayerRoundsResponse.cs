using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportPlayerRound;

public class PlayerRoundsResponse : MessageResponse
{
    public IEnumerable<RoundModel> Items { get; init; } = default!;

    public RoundTotalModel Total { get; init; } = default!;

    [JsonPropertyName("totalcount")]
	public int TotalCount { get; init; }
}