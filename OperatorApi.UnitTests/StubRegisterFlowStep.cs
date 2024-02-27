using System.Text.Json;
using TGS.Services.Operator;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Tests.OperatorApi.UnitTests;

public class StubRegisterFlowStep : IRegisterFlowStep
{
	private Func<string, bool> _confirmCompletedDelegate = _ => false;

	private Func<Guid, JsonDocument, PlayerLocationInfo, Task<RegisterFlowProcessResult>> _processInputDelegate =
		(_, _, _) => Task.FromResult(new RegisterFlowProcessResult(false, null, null));

	public bool IsAuto { get; set; } = false;

	public string Type { get; set; }

	public ValueTask<bool> ConfirmCompletedAsync(string playerCode, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(_confirmCompletedDelegate(playerCode));

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
		=> await _processInputDelegate(brandId, input, locationInfo).ConfigureAwait(false);

	internal void SetConfirmCompletedDelegate(Func<string, bool> confirmCompletedDelegate)
		=> _confirmCompletedDelegate = confirmCompletedDelegate;

	internal void SetProcessInputDelegate(Func<Guid, JsonDocument, PlayerLocationInfo, Task<RegisterFlowProcessResult>> processInputDelegate)
		=> _processInputDelegate = processInputDelegate;
}