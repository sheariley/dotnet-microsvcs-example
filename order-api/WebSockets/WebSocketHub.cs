using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using OpenTelemetry.Trace;

namespace order_api.WebSockets;

public class WebSocketHub(ILogger<WebSocketHub> logger)
{
    private readonly ConcurrentDictionary<string, List<WebSocket>> _sockets = new();

    public void Register(string customerId, WebSocket ws)
    {
        var list = _sockets.GetOrAdd(customerId, _ => []);
        lock (list) { list.Add(ws); }
    }

    public void Unregister(string customerId, WebSocket ws)
    {
        if (_sockets.TryGetValue(customerId, out var list))
            lock (list) { list.Remove(ws); }
    }

    public async Task PushAsync(string customerId, string message, CancellationToken ct = default)
    {
        if (!_sockets.TryGetValue(customerId, out var list)) return;

        WebSocket[] snapshot;
        lock (list) { snapshot = [.. list]; }

        var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        List<WebSocket> stale = [];

        foreach (var ws in snapshot)
        {
            if (ws.State == WebSocketState.Open)
            {
                try { await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct); }
                catch { stale.Add(ws); }
            }
            else { stale.Add(ws); }
        }

        if (stale.Count > 0)
            lock (list) { foreach (var ws in stale) list.Remove(ws); }
    }

    public async Task HandleConnectionRequest(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var customerId = context.Request.Query["customerId"].ToString();
        if (string.IsNullOrEmpty(customerId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var ws = await context.WebSockets.AcceptWebSocketAsync();
        Register(customerId, ws);

        try
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch (OperationCanceledException) { /* client disconnected — not an error */ }
        catch (Exception ex)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity.Current?.AddException(ex);
            logger.LogError(ex, "WebSocket error for customer {CustomerId}", customerId);
        }
        finally
        {
            Unregister(customerId, ws);
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error closing WebSocket for customer {CustomerId}", customerId);
                }
            }
        }
    }
}

public static class WebSocketHubExtensions
{
    public static void UseWebSocketHub(this WebApplication app, PathString path)
    {
        app.Map(path, (HttpContext context) =>
            context.RequestServices.GetRequiredService<WebSocketHub>().HandleConnectionRequest(context));
    }
}
