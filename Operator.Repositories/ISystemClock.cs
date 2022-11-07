using System;

namespace TGS.Operator.Repositories;

public interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
