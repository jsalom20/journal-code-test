namespace Library.Application.Abstractions;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}
