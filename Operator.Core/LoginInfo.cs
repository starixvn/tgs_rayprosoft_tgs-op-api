using System;

namespace TGS.Services.Operator;

public record LoginInfo(
	string PlayerToken,
	DateTimeOffset LoginTime);