using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetDeliveryLettersQueryService : IQueryService<GetDeliveryLettersQuery, DeliveryLetter>
{
	private readonly IStationLetterClient _stationLetterClient;

	public GetDeliveryLettersQueryService(IStationLetterClient stationLetterClient)
	{
		_stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));
	}

	public IAsyncEnumerable<DeliveryLetter> QueryAsync(GetDeliveryLettersQuery query, CancellationToken cancellationToken = default)
		=> _stationLetterClient.ListDeliverLettersAsync(query.PlayerCode, cancellationToken);
}
