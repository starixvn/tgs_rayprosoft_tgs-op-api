using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetCurrentPlayerRegisterFlowStepQueryService(IBrandRegisterFlowDefinitionManager brandRegisterFlowDefinitionManager) : IGetService<GetCurrentPlayerRegisterFlowStepQuery, string?>
{
	private readonly IBrandRegisterFlowDefinitionManager _brandRegisterFlowDefinitionManager = brandRegisterFlowDefinitionManager ?? throw new ArgumentNullException(nameof(brandRegisterFlowDefinitionManager));

	public async ValueTask<string?> GetAsync(GetCurrentPlayerRegisterFlowStepQuery query, CancellationToken cancellationToken = default)
	{
		var flowDefinition = await _brandRegisterFlowDefinitionManager
		  .GetRegisterFlowDefinitionAsync(query.BrandId, cancellationToken)
		  .ConfigureAwait(false);

		var flow = flowDefinition.BuildRegisterFlow();

		_ = await flow
			.ConfirmCompletedAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		return flow.CurrentStep;
	}
}