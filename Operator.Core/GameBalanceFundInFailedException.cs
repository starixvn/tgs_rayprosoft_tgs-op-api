using System;

namespace TGS.Services.Operator;

public class GameBalanceFundInFailedException : Exception
{
	public GameBalanceFundInFailedException(string errorMessage)
		: base($"The Game Balance Fund In Has Error ({errorMessage}).")
	{
	}
}
