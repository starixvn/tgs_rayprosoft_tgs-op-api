using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.DepositAccumulateRoulette.Clients;
using Tgs.ServiceUtility;

using Clients = Tgs.DepositAccumulateRoulette.Clients;

namespace TGS.Services.Operator.ApplicationServices.Promotions;

internal class PlayDepositAccumulateRouletteCommandService(
	IDepositAccumulateRouletteClient depositAccumulateRouletteClient) : IGetService<PlayDepositAccumulateRouletteCommand, Clients.RouletteResult>
{
	private readonly IDepositAccumulateRouletteClient _depositAccumulateRouletteClient = depositAccumulateRouletteClient ?? throw new ArgumentNullException(nameof(depositAccumulateRouletteClient));

	public ValueTask<Clients.RouletteResult> GetAsync(
		PlayDepositAccumulateRouletteCommand command,
		CancellationToken cancellationToken = default)
		=> _depositAccumulateRouletteClient.RouletteAsync(
			command.PromotionId,
			command.PlayerCode,
			cancellationToken);
}
