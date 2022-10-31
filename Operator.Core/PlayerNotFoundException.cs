using System;

namespace TGS.Services.Operator;

public class PlayerNotFoundException : Exception
{
	public PlayerNotFoundException(string message)
		: base(message)
	{
	}
}
