namespace DocumentAISample.Utils;
public interface ISystemClock
{
    public static ISystemClock Instance { get; } = new SystemClock();
    DateTimeOffset UtcNow();
}

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}