namespace McComms.WebSockets;

/// <summary>
/// WebSockets server implementation for handling client connections, 
/// message processing, and broadcasting.
/// </summary>
public sealed class WebSocketsServer : IDisposable {
    private const string DEFAULT_HOST = "127.0.0.1";
    private const int DEFAULT_PORT = 50051;
    private const int DEFAULT_BUFFER_SIZE = 1024;
    private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024; // 10MB max message size

    // HTTP listener for incoming WebSocket connections
    private readonly HttpListener _listener;

    // URL prefix for the WebSocket server
    private readonly string _socketUrl;

    // Thread-safe collection of connected WebSocket clients
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    // Counter for assigning unique client IDs.
    private int _nextClientId = 1;

    // Callback function for handling received messages from clients.
    private Func<byte[], byte[]>? _onMessageReceived;

    // Cancellation token source for stopping the server
    private readonly CancellationTokenSource _cts = new();

    // Semaphore for message processing synchronization
    private readonly SemaphoreSlim _messageProcessingLock = new(1, 1);

    /// The network address for the server.
    private readonly NetworkAddress? _address;

    /// <summary>
    /// Gets the network address for the server.
    /// </summary>
    public NetworkAddress? Address => _address;

    /// <summary>
    /// Indicates whether the server is currently running.
    /// </summary>
    public bool IsRunning { get; private set; } = false;

    /// <summary>
    /// Initializes a new instance of WebSocketsServer with default host and port.
    /// </summary>
    public WebSocketsServer() : this(new NetworkAddress(DEFAULT_HOST, DEFAULT_PORT)) { }

    /// <summary>
    /// Initializes a new instance of WebSocketsServer with specified IP address and port.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use for the server.</param>
    public WebSocketsServer(NetworkAddress address) {
        _address = address;
        _socketUrl = $"http://{_address.Host}:{_address.Port}/ws/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_socketUrl);
    }

    /// <summary>
    /// Begins listening for WebSocket connections and handling messages.
    /// </summary>
    /// <param name="onMessageReceived">Callback function for handling received messages.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ListenAsync(Func<byte[], byte[]>? onMessageReceived, CancellationToken cancellationToken) {
        if (IsRunning) {
            return;
        }

        _onMessageReceived = onMessageReceived;
        _listener.Start();
        IsRunning = true;
        Console.WriteLine($"WebSocket server listening on {_socketUrl}");

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
            _listener.Stop();
            IsRunning = false;
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
        // Use ArrayPool to rent a buffer instead of allocating a new one
        // More efficient than creating a new array for each client
        byte[] buffer = ArrayPool<byte>.Shared.Rent(DEFAULT_BUFFER_SIZE);
        
        // List to accumulate message fragments across multiple frames
        var messageFragments = new List<byte>();
        
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
                
                // Add this frame's data to the message fragments
                if (result.Count > 0) {
                    // Check if adding this frame would exceed the maximum message size
                    if (messageFragments.Count + result.Count > MAX_MESSAGE_SIZE) {
                        Console.WriteLine($"Message from client {clientId} exceeds maximum size ({MAX_MESSAGE_SIZE} bytes), closing connection");
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.MessageTooBig,
                            "Message too large",
                            cancellationToken);
                        break;
                    }
                    
                    for (int i = 0; i < result.Count; i++) {
                        messageFragments.Add(buffer[i]);
                    }
                }
                
                // Process the complete message when we receive the final frame
                if (result.EndOfMessage && messageFragments.Count > 0) {
                    byte[] completeMessage = messageFragments.ToArray();
                    messageFragments.Clear(); // Reset for next message
                    
                    // Process the complete message and send response
                    if (_onMessageReceived != null) {
                        byte[] response = _onMessageReceived(completeMessage);
                        await SendToClientAsync(webSocket, response, cancellationToken);
                    }
                }
                // If not end of message, continue accumulating fragments
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
            // Return the rented buffer to the pool
            ArrayPool<byte>.Shared.Return(buffer);
            
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
    /// Enhanced with error handling and cleanup like SocketsServer.
    /// </summary>
    /// <param name="data">The bytes to broadcast.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task BroadcastAsync(byte[] data, CancellationToken cancellationToken = default) {
        await _messageProcessingLock.WaitAsync(cancellationToken);
        try {
            int disconnectedCount = 0;
            var clientsToRemove = new List<string>();
            
            foreach (var clientPair in _clients) {
                var clientId = clientPair.Key;
                var webSocket = clientPair.Value;
                
                try {
                    if (webSocket.State == WebSocketState.Open) {
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(data, 0, data.Length),
                            WebSocketMessageType.Binary, 
                            true, 
                            cancellationToken);
                    }
                    else {
                        clientsToRemove.Add(clientId);
                    }
                }
                catch {
                    clientsToRemove.Add(clientId);
                    disconnectedCount++;
                }
            }

            // Clean up disconnected clients
            if (clientsToRemove.Count > 0) {
                foreach (var clientId in clientsToRemove) {
                    if (_clients.TryRemove(clientId, out var webSocket)) {
                        try {
                            webSocket.Dispose();
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error disposing WebSocket client {clientId}: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"{clientsToRemove.Count} WebSocket client(s) removed, remaining clients: {_clients.Count}");
            }

            if (disconnectedCount > 0) {
                Console.WriteLine($"{disconnectedCount} client(s) marked as disconnected and cleaned up");
            }
        }
        finally {
            _messageProcessingLock.Release();
        }
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
    /// Enhanced cleanup like SocketsServer.
    /// </summary>
    public void Dispose() {
        try {
            _cts.Cancel();
            _cts.Dispose();
            _listener.Close();
            
            foreach (var clientPair in _clients) {
                var clientId = clientPair.Key;
                var webSocket = clientPair.Value;
                try {
                    if (webSocket.State != WebSocketState.Closed) {
                        webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure, 
                            "Server shutting down", 
                            CancellationToken.None).Wait(1000); // Timeout after 1 second
                    }
                    webSocket.Dispose();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error disposing WebSocket client {clientId}: {ex.Message}");
                }
            }
            
            _clients.Clear();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error disposing WebSocketsServer: {ex.Message}");
        }
        finally {
            _messageProcessingLock?.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}
