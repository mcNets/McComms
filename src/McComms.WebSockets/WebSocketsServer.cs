namespace McComms.WebSockets;

/// <summary>
/// WebSockets server implementation for handling client connections, 
/// message processing, and broadcasting.
/// </summary>
public class WebSocketsServer : IDisposable {
    private readonly HttpListener _listener;
    private readonly string _url;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private int _nextClientId = 1;
    private Func<byte[], byte[]>? _onMessageReceived;
    private bool _isRunning = false;
    private readonly CancellationTokenSource _cts = new();
    private const int BUFFER_SIZE = 4096;

    /// <summary>
    /// Initializes a new instance of WebSocketsServer with default host and port.
    /// </summary>
    public WebSocketsServer() : this(IPAddress.Loopback, 8080) { }

    /// <summary>
    /// Initializes a new instance of WebSocketsServer with specified IP address and port.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use for the server.</param>
    public WebSocketsServer(IPAddress ipAddress, int port) {
        _url = $"http://{ipAddress}:{port}/ws/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_url);
    }

    /// <summary>
    /// Begins listening for WebSocket connections and handling messages.
    /// </summary>
    /// <param name="onMessageReceived">Callback function for handling received messages.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ListenAsync(Func<byte[], byte[]>? onMessageReceived, CancellationToken cancellationToken) {
        if (_isRunning) {
            return;
        }

        _onMessageReceived = onMessageReceived;
        _isRunning = true;
        _listener.Start();
        Console.WriteLine($"WebSocket server listening on {_url}");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        
        try {
            while (!linkedCts.Token.IsCancellationRequested) {
                HttpListenerContext context;
                
                try {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) when (linkedCts.Token.IsCancellationRequested) {
                    break;
                }

                if (context.Request.IsWebSocketRequest) {
                    ProcessWebSocketRequest(context, linkedCts.Token);
                }
                else {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (OperationCanceledException) {
            // Expected when cancellation is requested
        }
        finally {
            _isRunning = false;
            _listener.Stop();
        }
    }
    
    private async void ProcessWebSocketRequest(HttpListenerContext context, CancellationToken cancellationToken) {
        WebSocketContext webSocketContext;
        WebSocket webSocket;
        
        try {
            webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            webSocket = webSocketContext.WebSocket;
            
            string clientId = $"client_{_nextClientId++}";
            _clients.TryAdd(clientId, webSocket);
            Console.WriteLine($"Client {clientId} connected");
            
            await HandleClientAsync(clientId, webSocket, cancellationToken);
        }
        catch (Exception ex) {
            Console.WriteLine($"WebSocket processing error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }
    
    private async Task HandleClientAsync(string clientId, WebSocket webSocket, CancellationToken cancellationToken) {
        var buffer = new byte[BUFFER_SIZE];
        
        try {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close) {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Closing", 
                        cancellationToken);
                    break;
                }
                
                // Process the message if it's complete
                if (result.EndOfMessage) {
                    byte[] message = new byte[result.Count];
                    Array.Copy(buffer, message, result.Count);
                    
                    // Process the message and send response
                    if (_onMessageReceived != null) {
                        byte[] response = _onMessageReceived(message);
                        await SendToClientAsync(webSocket, response, cancellationToken);
                    }
                }
                // TODO: Handle large messages that span multiple frames
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) {
            Console.WriteLine($"Client {clientId} disconnected unexpectedly");
        }
        catch (OperationCanceledException) {
            // Expected when cancellation is requested
        }
        catch (Exception ex) {
            Console.WriteLine($"Error handling client {clientId}: {ex.Message}");
        }
        finally {
            if (webSocket.State != WebSocketState.Closed) {
                try {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Server shutting down", 
                        CancellationToken.None);
                }
                catch {
                    // Ignore any errors during forced close
                }
            }
            
            _clients.TryRemove(clientId, out _);
            webSocket.Dispose();
            Console.WriteLine($"Client {clientId} disconnected");
        }
    }
    
    /// <summary>
    /// Sends data to a specific WebSocket client.
    /// </summary>
    /// <param name="webSocket">The client WebSocket to send to.</param>
    /// <param name="data">The bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the send operation.</returns>
    private async Task SendToClientAsync(WebSocket webSocket, byte[] data, CancellationToken cancellationToken) {
        if (webSocket.State == WebSocketState.Open) {
            try {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(data, 0, data.Length),
                    WebSocketMessageType.Binary, 
                    true, 
                    cancellationToken);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// </summary>
    /// <param name="data">The bytes to broadcast.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task BroadcastAsync(byte[] data, CancellationToken cancellationToken = default) {
        var tasks = new List<Task>();
        
        foreach (var client in _clients.Values) {
            tasks.Add(SendToClientAsync(client, data, cancellationToken));
        }
        
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Stops the WebSockets server and closes all client connections.
    /// </summary>
    public void Stop() {
        _cts.Cancel();
        _listener.Stop();
    }
    
    /// <summary>
    /// Disposes the WebSockets server and related resources.
    /// </summary>
    public void Dispose() {
        _cts.Cancel();
        _cts.Dispose();
        _listener.Close();
        
        foreach (var client in _clients.Values) {
            try {
                client.Dispose();
            }
            catch {
                // Ignore disposal errors
            }
        }
        
        _clients.Clear();
    }
}
