using System;

namespace TGS.Services.Operator;

public record Login3rdInfo(
	string PlayerCode,
	string PlayerToken,
	DateTimeOffset LoginTime);