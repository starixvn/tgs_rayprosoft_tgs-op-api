using System;

namespace TGS.Services.Operator;

public class GameBalanceFundOutFailedException : Exception
{
	public GameBalanceFundOutFailedException(string errorMessage)
		: base($"The Game Balance Fund Out Has Error ({errorMessage}).")
	{
	}
}
