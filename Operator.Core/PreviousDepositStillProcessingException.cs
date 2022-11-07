using System;

namespace TGS.Services.Operator;

public class PreviousDepositStillProcessingException : Exception
{
	public PreviousDepositStillProcessingException() : base("Has other deposit processing")
	{
	}
}
