#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetDeliveryLetterQueryService(IStationLetterClient stationLetterClient) : IGetService<GetDeliveryLetterQuery, DeliveryLetter?>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public ValueTask<DeliveryLetter?> GetAsync(GetDeliveryLetterQuery query, CancellationToken cancellationToken = default)
		=> _stationLetterClient.GetDeliveryLetterAsync(query.DeliveryId, cancellationToken);
}