using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteSettingViewModel
{
    [JsonPropertyName("rouletteitems")]
    public IEnumerable<DepositAccumulateRouletteItemViewModel> RouletteItems { get; init; }
}
