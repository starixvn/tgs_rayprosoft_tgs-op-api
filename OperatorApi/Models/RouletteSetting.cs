#nullable enable

using System;
using System.Collections.Generic;

namespace TGS.Apis.OperatorApi.Models;

public class RouletteSetting
{
    public IEnumerable<RouletteItem> RouletteItems { get; init; } = Array.Empty<RouletteItem>();
}
