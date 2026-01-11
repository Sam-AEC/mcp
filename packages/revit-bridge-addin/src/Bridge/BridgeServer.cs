using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Serilog;
using RevitBridge.Bridge.Performance;

namespace RevitBridge.Bridge;

public class BridgeServer
{
    private readonly HttpListener _listener;
    private readonly CommandQueue _queue;
    private readonly ExternalEvent _externalEvent;
    private CancellationTokenSource _cts = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private Task? _listenerTask;
    private int _totalRequests = 0;
    private int _activeConnections = 0;

    // Performance features
    private readonly ResponseCache _cache;
    private readonly PerformanceMonitor _monitor;
    private readonly BatchProcessor _batchProcessor;

    public BridgeServer(CommandQueue queue, ExternalEvent externalEvent, string prefix = "http://127.0.0.1:3000/")
    {
        _queue = queue;
        _externalEvent = externalEvent;
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        // Initialize performance features
        _cache = new ResponseCache(maxEntries: 1000, defaultTtlSeconds: 60);
        _monitor = new PerformanceMonitor();
        _batchProcessor = new BatchProcessor(null, _cache); // UIApp set later
    }

    public bool IsListening { get; private set; }
    public bool IsRunning => IsListening;
    public int ActiveConnections => _activeConnections;
    public int TotalRequests => _totalRequests;
    public double UptimeSeconds => (DateTime.UtcNow - _startTime).TotalSeconds;

    public void Start()
    {
        if (IsListening) return;

        try
        {
            if (_cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();

            _listener.Start();
            _listenerTask = Task.Run(ListenerLoop, _cts.Token);
            IsListening = true;
            Log.Information("BridgeServer started on {Prefixes}", string.Join(", ", _listener.Prefixes));
        }
        catch (Exception ex)
        {
             Log.Error(ex, "Failed to start listener");
             IsListening = false;
        }
    }

    public void Stop()
    {
        if (!IsListening) return;

        try
        {
            _cts.Cancel();
            _listener.Stop();
            // _listenerTask?.Wait(5000); // Avoid blocking UI thread
            IsListening = false;
            Log.Information("BridgeServer stopped");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stop listener");
        }
    }

    private async Task ListenerLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context), _cts.Token);
            }
            catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Listener error");
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        Interlocked.Increment(ref _activeConnections);
        Interlocked.Increment(ref _totalRequests);

        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";

            if (path == "/health")
            {
                await HandleHealth(context);
            }
            else if (path == "/tools")
            {
                await HandleTools(context);
            }
            else if (path == "/execute" && context.Request.HttpMethod == "POST")
            {
                await HandleExecute(context);
            }
            else if (path == "/performance/stats")
            {
                await HandlePerformanceStats(context);
            }
            else if (path == "/performance/cache/stats")
            {
                await HandleCacheStats(context);
            }
            else if (path == "/performance/cache/clear" && context.Request.HttpMethod == "POST")
            {
                await HandleCacheClear(context);
            }
            else if (path == "/batch" && context.Request.HttpMethod == "POST")
            {
                await HandleBatch(context);
            }
            else
            {
                Respond(context, 404, new { error = "Not found" });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Request handling error");
            Respond(context, 500, new { error = ex.Message });
        }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
        }
    }

    private async Task HandleExecute(HttpListenerContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        using var reader = new StreamReader(context.Request.InputStream);
        var body = await reader.ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var requestId = root.GetProperty("request_id").GetString() ?? Guid.NewGuid().ToString();
        var tool = root.GetProperty("tool").GetString() ?? string.Empty;
        var payload = root.GetProperty("payload");

        // Check cache for read-only commands
        if (IsReadOnlyCommand(tool))
        {
            var cacheKey = ResponseCache.GenerateKey(tool, payload.ToString());
            if (_cache.TryGet(cacheKey, out var cachedResponse))
            {
                sw.Stop();
                _monitor.RecordCommand(tool, sw.ElapsedMilliseconds, true);
                Log.Information("Cache hit: {Tool} {DurationMs}ms", tool, sw.ElapsedMilliseconds);
                Respond(context, 200, new
                {
                    status = "success",
                    result = cachedResponse,
                    cached = true,
                    execution_time_ms = sw.ElapsedMilliseconds
                });
                return;
            }
        }

        var request = new CommandRequest
        {
            RequestId = requestId,
            Tool = tool,
            Payload = payload
        };

        Log.Information("Request received: {RequestId} {Tool} from {ClientIP}",
            requestId, tool, context.Request.RemoteEndPoint?.Address.ToString());

        _queue.Enqueue(request);
        _externalEvent.Raise();

        var response = await _queue.WaitForResponse(requestId);
        sw.Stop();

        // Record performance metrics
        bool success = response.Status == "success";
        _monitor.RecordCommand(tool, sw.ElapsedMilliseconds, success, success ? null : response.Error?.ToString());

        // Cache successful read-only responses
        if (success && IsReadOnlyCommand(tool))
        {
            var cacheKey = ResponseCache.GenerateKey(tool, payload.ToString());
            _cache.Set(cacheKey, response.Result);
        }

        Log.Information("Request completed: {RequestId} {Tool} {Status} {DurationMs}ms",
            requestId, tool, response.Status, sw.ElapsedMilliseconds);

        Respond(context, 200, response);
    }

    private bool IsReadOnlyCommand(string tool)
    {
        return tool.StartsWith("revit.get_") ||
               tool.StartsWith("revit.list_") ||
               tool == "revit.health" ||
               tool.Contains("_info") ||
               tool.Contains("_statistics") ||
               tool.Contains("_summary");
    }

    private Task HandleHealth(HttpListenerContext context)
    {
        var health = new
        {
            status = "healthy",
            version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            uptime_seconds = (DateTime.UtcNow - _startTime).TotalSeconds,
            revit_version = App.RevitVersion ?? "unknown",
            active_document = App.ActiveDocumentName ?? "none"
        };
        Respond(context, 200, health);
        return Task.CompletedTask;
    }

    private Task HandleTools(HttpListenerContext context)
    {
        var tools = BridgeCommandFactory.GetToolCatalog();
        Respond(context, 200, new { tools });
        return Task.CompletedTask;
    }

    private Task HandlePerformanceStats(HttpListenerContext context)
    {
        var stats = _monitor.GetStatistics();
        Respond(context, 200, stats);
        return Task.CompletedTask;
    }

    private Task HandleCacheStats(HttpListenerContext context)
    {
        var stats = _cache.GetStats();
        Respond(context, 200, stats);
        return Task.CompletedTask;
    }

    private Task HandleCacheClear(HttpListenerContext context)
    {
        _cache.Clear();
        Respond(context, 200, new { success = true, message = "Cache cleared successfully" });
        return Task.CompletedTask;
    }

    private async Task HandleBatch(HttpListenerContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<JsonElement>(body);

            // Queue batch execution
            var completionSource = new TaskCompletionSource<object>();
            _queue.Enqueue(new CommandRequest
            {
                Tool = "batch_execute",
                Payload = payload,
                CompletionSource = completionSource
            });

            _externalEvent.Raise();

            var result = await completionSource.Task;
            Respond(context, 200, result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Batch execution error");
            Respond(context, 500, new { error = ex.Message });
        }
    }

    private void Respond(HttpListenerContext context, int statusCode, object data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data);
        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }
}
