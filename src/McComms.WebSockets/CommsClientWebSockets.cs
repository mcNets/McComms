// filepath: c:\Code\McComms\src\McComms.WebSockets\CommsClientWebSockets.cs
namespace McComms.WebSockets;

/// <summary>
/// WebSockets implementation of the ICommsClient interface.
/// Provides client-side functionality for WebSocket-based communications.
/// </summary>
public class CommsClientWebSockets : ICommsClient {
    private readonly WebSocketsClient _webSocketClient;
    private Action<BroadcastMessage>? _onBroadcastReceived;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the CommsClientWebSockets class with default server address and port.
    /// </summary>
    public CommsClientWebSockets() {
        _webSocketClient = new WebSocketsClient();
    }

    /// <summary>
    /// Initializes a new instance of the CommsClientWebSockets class with specified server address and port.
    /// </summary>
    /// <param name="serverAddress">The server IP address to connect to.</param>
    /// <param name="port">The port number on which the server is listening.</param>
    public CommsClientWebSockets(IPAddress serverAddress, int port) {
        _webSocketClient = new WebSocketsClient(serverAddress, port);
    }

    /// <summary>
    /// Connects to the server and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    public bool Connect(Action<BroadcastMessage> onBroadcastReceived) {
        _onBroadcastReceived = onBroadcastReceived;
        
        try {
            return _webSocketClient.ConnectAsync(OnMessageReceived).GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error connecting to WebSocket server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the WebSocket server.
    /// </summary>
    public void Disconnect() {
        try {
            _webSocketClient.DisconnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error disconnecting from WebSocket server: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a command to the server and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <returns>The response from the server.</returns>
    public CommandResponse SendCommand(CommandRequest msg) {
        try {
            var json = JsonSerializer.Serialize(msg, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes($"C:{json}"); // Prefix with 'C:' to indicate command
            
            var responseData = _webSocketClient.SendAndReceiveAsync(bytes).GetAwaiter().GetResult();
            
            if (responseData != null) {
                var responseString = Encoding.UTF8.GetString(responseData);
                
                // Check if response starts with "R:" prefix
                if (responseString.StartsWith("R:")) {
                    var responseJson = responseString.Substring(2); // Remove the "R:" prefix
                    var response = JsonSerializer.Deserialize<CommandResponse>(responseJson, _jsonOptions);
                    
                    if (response != null) {
                        return response;
                    }
                }
            }
            
            return new CommandResponse(false, "", "Failed to get valid response from server");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error sending command: {ex.Message}");
            return new CommandResponse(false, "", $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an exit command to the server.
    /// </summary>
    public void SendExitCommand() {
        try {
            var bytes = Encoding.UTF8.GetBytes("E:"); // Simple exit command
            _ = _webSocketClient.SendAsync(bytes).GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error sending exit command: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles incoming messages from the server.
    /// </summary>
    /// <param name="data">The received data.</param>
    /// <returns>A task representing the async operation.</returns>
    private Task OnMessageReceived(byte[] data) {
        try {
            var message = Encoding.UTF8.GetString(data);
            
            // Check if this is a broadcast message (starts with "B:")
            if (message.StartsWith("B:") && _onBroadcastReceived != null) {
                var broadcastJson = message.Substring(2); // Remove the "B:" prefix
                var broadcast = JsonSerializer.Deserialize<BroadcastMessage>(broadcastJson, _jsonOptions);
                
                if (broadcast != null) {
                    _onBroadcastReceived(broadcast);
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error processing received message: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
}
