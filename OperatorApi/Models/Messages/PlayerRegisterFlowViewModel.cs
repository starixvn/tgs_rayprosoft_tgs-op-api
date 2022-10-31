using System.Text.Json;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PlayerRegisterFlowViewModel
{
	public JsonDocument Input { get; set; } = default!;

	public string Type { get; set; } = default!;
}
