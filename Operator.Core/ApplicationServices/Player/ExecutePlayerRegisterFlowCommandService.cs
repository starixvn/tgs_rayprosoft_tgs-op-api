using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ExecutePlayerRegisterFlowCommandService(IBrandRegisterFlowDefinitionManager brandRegisterFlowDefinitionManager) : IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult>
{
	private readonly IBrandRegisterFlowDefinitionManager _brandRegisterFlowDefinitionManager = brandRegisterFlowDefinitionManager ?? throw new ArgumentNullException(nameof(brandRegisterFlowDefinitionManager));

	public async ValueTask<RegisterFlowProcessResult> GetAsync(
		ExecutePlayerRegisterFlowCommand command,
		CancellationToken cancellationToken = default)
	{
		var flowDefinition = await _brandRegisterFlowDefinitionManager
			.GetRegisterFlowDefinitionAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false);

		var flow = flowDefinition.BuildRegisterFlow();

		var isCompleted = await flow
			.ConfirmCompletedAsync(command.LocationInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (isCompleted)
			return new RegisterFlowProcessResult(
				true,
				null,
				null);

		if (command.StepType != flow.CurrentStep)
			throw new Exception("Input type not match current step type");

		var result = await flow.ProcessInputAsync(
			command.BrandId,
			command.Input,
			command.LocationInfo,
			cancellationToken).ConfigureAwait(false);

		return result;
	}
}