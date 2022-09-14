using System.Text.Json;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PlayerRegisterFlowResponseViewModel
{
    public bool IsCompleted { get; set; }

    public string? NextStepType { get; set; }

    public JsonDocument? StepResponse { get; set; }
}
