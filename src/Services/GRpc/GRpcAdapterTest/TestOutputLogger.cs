using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace GRpcAdapterTest;

class TestOutputLogger : ILogger
{
    private ITestOutputHelper _output;

    public TestOutputLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = $"[{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}][{logLevel}][{eventId}]: {formatter(state, exception)}";
        if (exception != null)
        {
            msg += $"\n{exception}";
        }
        _output.WriteLine(msg);
    }
}
