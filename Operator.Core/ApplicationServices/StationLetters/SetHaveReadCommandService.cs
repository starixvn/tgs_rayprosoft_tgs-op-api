using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class SetHaveReadCommandService : ICommandService<SetHaveReadCommand>
{
	private readonly IStationLetterRepository _stationLetterRepository;

	public SetHaveReadCommandService(IStationLetterRepository stationLetterRepository)
	{
		_stationLetterRepository = stationLetterRepository ??
			throw new ArgumentNullException(nameof(stationLetterRepository));
	}

	public ValueTask ExecuteAsync(SetHaveReadCommand command, CancellationToken cancellationToken = default) =>
		_stationLetterRepository.SetStationLetterHaveReadAsync(command.PlayerCode, command.LetterId);
}
