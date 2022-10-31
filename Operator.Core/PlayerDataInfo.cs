namespace TGS.Services.Operator;

public record PlayerDataInfo(
	string PlayerCode,
	string UserName,
	string RealName,
	string Phone,
	string Email,
	int VipLevel,
	string QQ,
	string Wechat,
	bool IsSetPayPassword,
	bool IdentityAuthentication
);
