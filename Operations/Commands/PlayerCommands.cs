using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operations.Commands;

public sealed class PlayerCommands(
	ITgsRepository repository) : IPlayerCommands
{
	private readonly ITgsRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

	public async Task ChangePlayerPasswordAsync(ChangePlayerPasswordData data)
	{
		var player = await _repository.Players.FindAsync(data.PlayerCode).ConfigureAwait(false);

		player.Salt = data.Salt;
		player.Password = data.Password;
		player.UpdatedOn = data.UpdatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public Task DeauthorizePlayerAsync(string playerCode, Guid loginRecordId)
	{
		const string sql = "call dbo.player_deauthorize_by_login_record_id(:player_code, :login_record_id)";

		return _repository.ExecuteSqlCommandAsync(sql,
			CommandType.Text,
			new Dictionary<string, object>
			{
				{
					":player_code", playerCode
				},
				{
					":login_record_id", loginRecordId
				}
			});
	}

	public Task DeauthorizePlayerAsync(string playerCode, PlatformType? platformType)
	{
		const string sql = "call dbo.player_deauthorize_by_platform_type";

		return _repository.ExecuteSqlCommandAsync(
			sql,
			CommandType.Text,
			new Dictionary<string, object>
			{
				{
					":player_code", playerCode
				},
				{
					":platform_type", platformType
				}
			});
	}

	public async Task<PlayerLoginRecord> PlayerLoginAsync(PlayerLoginData data)
	{
		var loginRecord = new PlayerLoginRecord
		{
			Id = Guid.NewGuid(),
			PlayerCode = data.PlayerCode,
			BrandId = data.BrandId,
			Status = LoginStatus.Enabled,
			PlatformType = data.PlatformType,
			IpAddress = data.IpAddress,
			CreatedOn = data.CreatedOn,
			ExpiredOn = data.ExpiredOn
		};

		_ = await _repository.PlayerLoginRecords.AddAsync(loginRecord);
		_ = await _repository.SaveChangesAsync();

		return loginRecord;
	}

	public async Task<int> SetEmailAsync(string playerCode, string email, DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.Email = email;
		player.UpdatedOn = updatedOn;

		return await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task SetIdentityAsync(
		string playerCode, string realName, string identity, bool isAuthenticate,
		DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.RealName = realName;
		player.IdNumber = identity;
		player.IdentityAuthentication = isAuthenticate;
		player.UpdatedOn = updatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task SetPayPasswordAsync(
		string playerCode, byte[] newPassword, byte[] newSalt, DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.WithdrawSalt = newSalt;
		player.WithdrawPassword = newPassword;
		player.UpdatedOn = updatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task SetPhoneAsync(string playerCode, string areaCode, string phone, DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.Phone = phone;
		player.AreaCode = areaCode;
		player.PhoneAuthentication = true;
		player.UpdatedOn = updatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task SetQqAsync(string playerCode, string qqId, DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.QQ = qqId;
		player.UpdatedOn = updatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async Task SetWeChatAsync(string playerCode, string weChatId, DateTimeOffset updatedOn)
	{
		var player = await _repository.Players.FindAsync(playerCode).ConfigureAwait(false);

		player.Wechat = weChatId;
		player.UpdatedOn = updatedOn;

		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}
}