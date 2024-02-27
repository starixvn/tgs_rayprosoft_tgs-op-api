using System;

namespace TGS.Services.Operator;

public class ChangePasswordInfo
{
	public string PlayerCode { get; set; } = default!;

	public DateTimeOffset UpdateTime { get; set; }
}