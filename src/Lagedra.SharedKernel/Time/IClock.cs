namespace Lagedra.SharedKernel.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}
