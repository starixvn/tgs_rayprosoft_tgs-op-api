using System;

namespace TGS.Services.Operator;

public class GameBalanceFundOutFailedException(string errorMessage) : Exception($"The Game Balance Fund Out Has Error ({errorMessage}).")
{
}
