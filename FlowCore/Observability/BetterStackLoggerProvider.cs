using System.Collections;
using System.Threading.Channels;

namespace FlowCore.Observability;

/// <summary>
/// Best-effort Better Stack sink. It never delays or fails an application request.
/// Configure with BetterStack:SourceToken and BetterStack:IngestingHost.
/// </summary>
public sealed class BetterStackLoggerProvider : ILoggerProvider
{
    private readonly Channel<Dictionary<string, object?>> _events = Channel.CreateBounded<Dictionary<string, object?>>(
        new BoundedChannelOptions(1_000) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly HttpClient _client;
    private readonly Task _sender;
    private readonly CancellationTokenSource _shutdown = new();
    private int _deliveryFailureReported;

    public BetterStackLoggerProvider(string sourceToken, string ingestingHost)
    {
        _client = new HttpClient { BaseAddress = new Uri($"https://{ingestingHost.Trim().TrimEnd('/')}/") };
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sourceToken);
        _sender = SendAsync();
    }

    public ILogger CreateLogger(string categoryName) => new BetterStackLogger(categoryName, _events.Writer);

    public void Dispose()
    {
        _events.Writer.TryComplete();
        _shutdown.Cancel();
        try { _sender.Wait(TimeSpan.FromSeconds(2)); } catch { /* best-effort shutdown */ }
        _client.Dispose();
        _shutdown.Dispose();
    }

    private async Task SendAsync()
    {
        try
        {
            await foreach (var logEvent in _events.Reader.ReadAllAsync(_shutdown.Token))
            {
                try
                {
                    using var response = await _client.PostAsJsonAsync("", logEvent, _shutdown.Token);
                    if (!response.IsSuccessStatusCode)
                        ReportDeliveryFailure($"HTTP {(int)response.StatusCode}");
                }
                catch (OperationCanceledException) { /* Logging must not affect the application. */ }
                catch (HttpRequestException) { ReportDeliveryFailure("network error"); }
                catch (Exception) { ReportDeliveryFailure("unexpected error"); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ReportDeliveryFailure(string reason)
    {
        if (Interlocked.Exchange(ref _deliveryFailureReported, 1) == 0)
            Console.Error.WriteLine($"Better Stack log delivery failed ({reason}). Check the source token and ingesting host.");
    }

    private sealed class BetterStackLogger(string categoryName, ChannelWriter<Dictionary<string, object?>> writer) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var entry = new Dictionary<string, object?>
            {
                ["dt"] = DateTimeOffset.UtcNow,
                ["level"] = logLevel.ToString(),
                ["message"] = formatter(state, exception),
                ["category"] = categoryName,
                ["event_id"] = eventId.Id,
                ["exception"] = exception?.ToString()
            };

            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                foreach (var (key, value) in properties)
                {
                    if (key != "{OriginalFormat}") entry[key] = value;
                }
            }

            writer.TryWrite(entry);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
