using System;

namespace TGS.Services.Operator;

public record LoginInfo(
	string PlayerCode,
	string PlayerToken,
	DateTimeOffset LoginTime);
