using System;
using System.Collections.Generic;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class RouletteSettingsViewModel
{
    public IEnumerable<RouletteItemViewModel> Items { get; set; } = Array.Empty<RouletteItemViewModel>();
}