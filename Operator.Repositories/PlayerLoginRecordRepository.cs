using System;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class PlayerLoginRecordRepository(
	ITgsRepository tgsRepository) : IPlayerLoginRecordRepository
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async ValueTask<LoginRecordInfo> AddPlayerLoginRecordAsync(Guid brandId, string playerCode, PlatformType platformType, string ipAddress)
	{
		const int expiredSeconds = 3600 * 12;

		var loginRecord = new PlayerLoginRecord
		{
			Id = Guid.NewGuid(),
			PlayerCode = playerCode,
			BrandId = brandId,
			Status = LoginStatus.Enabled,
			PlatformType = platformType,
			IpAddress = ipAddress,
			CreatedOn = DateTimeOffset.UtcNow,
			ExpiredOn = DateTimeOffset.UtcNow.AddSeconds(expiredSeconds)
		};

		_ = await _tgsRepository.PlayerLoginRecords.AddAsync(loginRecord);
		_ = await _tgsRepository.SaveChangesAsync();

		return new LoginRecordInfo(
			loginRecord.Id,
			loginRecord.PlayerCode,
			loginRecord.Status,
			loginRecord.PlatformType,
			loginRecord.CreatedOn,
			loginRecord.ExpiredOn);
	}

	public async ValueTask DeauthorizePlayerAsync(Guid loginRecordId)
	{
		var loginRecord = await _tgsRepository.PlayerLoginRecords
			.FindAsync(loginRecordId)
			.ConfigureAwait(false);

		loginRecord!.Status = LoginStatus.Deauthorized;

		_ = _tgsRepository.PlayerLoginRecords.Update(loginRecord);
		_ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async ValueTask<LoginRecordInfo?> GetPlayerLoginRecordAsync(Guid loginRecordId)
	{
		var loginRecord = await _tgsRepository.PlayerLoginRecords
			.FindAsync(loginRecordId)
			.ConfigureAwait(false);

		return loginRecord == null
			? null
			: new LoginRecordInfo(
				loginRecord.Id,
				loginRecord.PlayerCode,
				loginRecord.Status,
				loginRecord.PlatformType,
				loginRecord.CreatedOn,
				loginRecord.ExpiredOn);
	}
}