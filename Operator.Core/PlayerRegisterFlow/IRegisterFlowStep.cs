using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Services.Operator.PlayerRegisterFlow;

public interface IRegisterFlowStep
{
	string? CurrentStep => Type;

	bool IsAuto => false;

	string Type { get; }

	ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default);

	ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default);

	void SetupStep(JsonElement jsonElement) { }
}
