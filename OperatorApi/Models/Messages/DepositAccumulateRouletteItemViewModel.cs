using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteItemViewModel
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = default!;
}
