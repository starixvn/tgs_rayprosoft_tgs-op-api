using System;

namespace TGS.Operator.Repositories;

internal class DefaultSystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
