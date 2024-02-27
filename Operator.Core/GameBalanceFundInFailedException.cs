using System;

namespace TGS.Services.Operator;

public class GameBalanceFundInFailedException(string errorMessage) : Exception($"The Game Balance Fund In Has Error ({errorMessage}).")
{
}