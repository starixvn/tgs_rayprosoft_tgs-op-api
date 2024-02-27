using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetDeliveryLettersQueryService(IStationLetterClient stationLetterClient) : IQueryService<GetDeliveryLettersQuery, DeliveryLetter>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public IAsyncEnumerable<DeliveryLetter> QueryAsync(GetDeliveryLettersQuery query, CancellationToken cancellationToken = default)
		=> _stationLetterClient.ListDeliverLettersAsync(query.PlayerCode, cancellationToken);
}