using System.Collections.Generic;
using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator;

public class PlayerRoundsResponse : MessageResponse
{
    public IEnumerable<RoundModel> Items { get; init; } = default!;

    public RoundTotalModel Total { get; init; } = default!;

    [JsonPropertyName("totalcount")]
	public int TotalCount { get; init; }
}