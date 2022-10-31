using System;
using Tgs.PlayerService.Client;

namespace TGS.Services.Operator;

public record PlayerInfo(
	string PlayerCode,
	Guid BrandId,
	decimal DepositedAmount,
	bool IdentityAuthentication,
	bool PhoneAuthentication,
	string? Phone,
	string? AreaCode,
	Guid? VipLevelId,
	PlayerStatus Status,
	string UserName,
	string? RealName,
	string? Email,
	int VipLevel,
	string? QQ,
	string? Wechat,
	bool IsSetPayPassword);
