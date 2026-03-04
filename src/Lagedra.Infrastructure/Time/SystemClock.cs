using Lagedra.SharedKernel.Time;

namespace Lagedra.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
