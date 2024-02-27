using System;

namespace TGS.Services.Operator;

public class PlayerNotFoundException(string message) : Exception(message)
{
}