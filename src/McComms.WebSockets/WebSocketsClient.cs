namespace McComms.WebSockets;

/// <summary>
/// Client implementation for WebSocket communications.
/// </summary>
public class WebSocketsClient : IDisposable {    private readonly string _serverUrl;
    private ClientWebSocket? _webSocket;
    private Func<byte[], Task>? _onMessageReceived;
    private readonly CancellationTokenSource _cts = new();
    private bool _isRunning = false;
    private const int BUFFER_SIZE = 4096;
    
    /// <summary>
    /// Gets a value indicating whether the client is currently connected and running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new WebSocketsClient with default server host and port.
    /// </summary>
    public WebSocketsClient() : this(IPAddress.Parse(DefaultNetworkSettings.DEFAULT_HOST), DefaultNetworkSettings.DEFAULT_PORT) { }

    /// <summary>
    /// Initializes a new WebSocketsClient with the specified server host and port.
    /// </summary>
    /// <param name="serverAddress">Server IP address.</param>
    /// <param name="port">Server port number.</param>
    public WebSocketsClient(IPAddress serverAddress, int port) {
        _serverUrl = $"ws://{serverAddress}:{port}/ws/";
    }

    /// <summary>
    /// Initializes a new WebSocketsClient with the specified server URL.
    /// </summary>
    /// <param name="serverUrl">The complete WebSocket server URL.</param>
    public WebSocketsClient(string serverUrl) {
        _serverUrl = serverUrl;
    }

    /// <summary>
    /// Connects to the WebSocket server.
    /// </summary>
    /// <param name="onMessageReceived">Callback for handling messages received from the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    public async Task<bool> ConnectAsync(Func<byte[], Task>? onMessageReceived, CancellationToken cancellationToken = default) {
        if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)) {
            return true;
        }

        _webSocket = new ClientWebSocket();
        
        try {
            await _webSocket.ConnectAsync(new Uri(_serverUrl), cancellationToken);
            _onMessageReceived = onMessageReceived;
            
            if (_webSocket.State == WebSocketState.Open) {
                _isRunning = true;
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                return true;
            }
            
            return false;
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to connect to WebSocket server: {ex.Message}");
            _webSocket.Dispose();
            _webSocket = null;
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the WebSocket server.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DisconnectAsync() {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) {
            return;
        }

        _cts.Cancel();
        
        try {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure, 
                "Client closing connection", 
                CancellationToken.None);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error during WebSocket disconnect: {ex.Message}");
        }
        finally {
            _webSocket.Dispose();
            _webSocket = null;
            _isRunning = false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken) {
        var buffer = new byte[BUFFER_SIZE];
        
        while (_webSocket != null && _webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
            try {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close) {
                    break;
                }

                if (result.EndOfMessage) {
                    byte[] message = new byte[result.Count];
                    Array.Copy(buffer, message, result.Count);
                    
                    if (_onMessageReceived != null) {
                        await _onMessageReceived(message);
                    }
                }
                // TODO: Handle large messages that span multiple frames
            }
            catch (OperationCanceledException) {
                // Expected when cancellation is requested
                break;
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) {
                Console.WriteLine("WebSocket connection closed unexpectedly");
                break;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error receiving WebSocket message: {ex.Message}");
                break;
            }
        }
        
        _isRunning = false;
    }

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken = default) {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) {
            return false;
        }

        try {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(data, 0, data.Length),
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
            
            return true;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error sending WebSocket message: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a message to the server and waits for a response.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The server's response or null if no response received.</returns>
    public async Task<byte[]?> SendAndReceiveAsync(byte[] data, CancellationToken cancellationToken = default) {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) {
            return null;
        }

        // Create a TaskCompletionSource for the response
        var responseCompletionSource = new TaskCompletionSource<byte[]>();
        
        // Store the original callback
        var originalCallback = _onMessageReceived;
        
        // Set up a temporary callback that captures the response and restores the original callback
        _onMessageReceived = async (responseData) => {
            // Restore original callback
            _onMessageReceived = originalCallback;
            
            // Complete the task with the response
            responseCompletionSource.TrySetResult(responseData);
            
            // Forward to original callback if it exists
            if (originalCallback != null) {
                await originalCallback(responseData);
            }
        };
        
        try {
            // Send the request
            if (!await SendAsync(data, cancellationToken)) {
                _onMessageReceived = originalCallback;
                return null;
            }
            
            // Wait for the response with a timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, cancellationToken);
            
            try {
                return await responseCompletionSource.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) {
                _onMessageReceived = originalCallback;
                if (timeoutCts.IsCancellationRequested) {
                    Console.WriteLine("Request timed out");
                }
                return null;
            }
        }
        catch (Exception ex) {
            _onMessageReceived = originalCallback;
            Console.WriteLine($"Error in SendAndReceive: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Disposes the WebSocket client resources.
    /// </summary>
    public void Dispose() {
        _cts.Cancel();
        _cts.Dispose();
        
        if (_webSocket != null) {
            if (_webSocket.State == WebSocketState.Open) {
                try {
                    // Force synchronous close for cleanup
                    _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disposing client",
                        CancellationToken.None).GetAwaiter().GetResult();
                }
                catch {
                    // Ignore errors during forced close
                }
            }
            
            _webSocket.Dispose();
            _webSocket = null;
        }
        
        _isRunning = false;
    }
}
