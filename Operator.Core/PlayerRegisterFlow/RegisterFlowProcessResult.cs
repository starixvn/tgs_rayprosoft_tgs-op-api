using System.Text.Json;

namespace TGS.Services.Operator.PlayerRegisterFlow;

public record RegisterFlowProcessResult(
	bool IsCompleted,
	string? NextStepType,
	JsonDocument? StepResponse);
