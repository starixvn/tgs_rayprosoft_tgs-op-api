using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ValidatePlayerLoginRecordCommandService : ICommandService<ValidatePlayerLoginRecordCommand>
{
	private readonly IPlayerLoginRecordRepository _loginRecordRepository;

	public ValidatePlayerLoginRecordCommandService(
		IPlayerLoginRecordRepository loginRecordRepository)
	{
		_loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
	}

	public async ValueTask ExecuteAsync(ValidatePlayerLoginRecordCommand command, CancellationToken cancellationToken = default)
	{
		var loginRecord = await _loginRecordRepository
			.GetPlayerLoginRecordAsync(command.LoginRecordId)
			.ConfigureAwait(false);

		if (loginRecord == null)
		{
			throw new PlayerLoginExpiredException(command.PlayerCode);
		}

		if (loginRecord.PlayerCode != command.PlayerCode)
		{
			throw new PlayerLoginExpiredException(command.PlayerCode);
		}

		if (loginRecord.Status != LoginStatus.Enabled)
		{
			throw new PlayerLoginExpiredException(command.PlayerCode);
		}

		if (loginRecord.ExpiredOn < DateTimeOffset.UtcNow)
		{
			throw new PlayerLoginExpiredException(command.PlayerCode);
		}
	}
}
