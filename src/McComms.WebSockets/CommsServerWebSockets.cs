namespace McComms.WebSockets;

/// <summary>
/// WebSockets implementation of the ICommsServer interface.
/// Provides server-side functionality for WebSocket-based communications.
/// </summary>
public class CommsServerWebSockets : ICommsServer {
    private readonly WebSocketsServer _webSocketServer;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    

    public CommsProtocol Protocol => CommsProtocol.WebSockets;
    
    /// <summary>
    /// Initializes a new instance of the CommsServerWebSockets class with default host and port.
    /// </summary>
    public CommsServerWebSockets() {
        _webSocketServer = new WebSocketsServer();
    }

    /// <summary>
    /// Initializes a new instance of the CommsServerWebSockets class with specified host and port.
    /// </summary>
    /// <param name="address">The NetworkAddress to listen on.</param>
    public CommsServerWebSockets(NetworkAddress address) {
        _webSocketServer = new WebSocketsServer(address);
    }

    /// <summary>
    /// Gets or sets the callback function that is invoked when a command is received.
    /// </summary>
    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    public NetworkAddress Address => throw new NotImplementedException();

    /// <summary>
    /// Starts the server and begins listening for incoming connections and commands.
    /// </summary>
    /// <param name="commandReceived">Callback function invoked when a command is received.</param>
    /// <param name="stoppingToken">A cancellation token that can be used to stop the server.</param>
    public void Start(Func<CommandRequest, CommandResponse>? commandReceived, CancellationToken stoppingToken) {
        CommandReceived = commandReceived;
        _ = Task.Run(async () => await _webSocketServer.ListenAsync(OnCommandReceived, stoppingToken), stoppingToken);
    }

    /// <summary>
    /// Stops the WebSockets server.
    /// </summary>
    public void Stop() {
        _webSocketServer.Stop();
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients.
    /// </summary>
    /// <param name="msg">The broadcast message to send.</param>
    public void SendBroadcast(BroadcastMessage msg) {
        var json = JsonSerializer.Serialize(msg, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes($"B:{json}"); // Prefix with 'B:' to indicate broadcast
        _ = _webSocketServer.BroadcastAsync(bytes);
    }

    /// <summary>
    /// Handles incoming command messages from clients.
    /// </summary>
    /// <param name="data">The received command data.</param>
    /// <returns>The response data to send back to the client.</returns>
    private byte[] OnCommandReceived(byte[] data) {
        try {
            var message = Encoding.UTF8.GetString(data);
            
            // Check if this is a command message (starts with "C:")
            if (message.StartsWith("C:")) {
                var commandJson = message.Substring(2); // Remove the "C:" prefix
                var command = JsonSerializer.Deserialize<CommandRequest>(commandJson, _jsonOptions);

                if (command != null && CommandReceived != null) {
                    var response = CommandReceived(command);
                    var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                    return Encoding.UTF8.GetBytes($"R:{responseJson}"); // Prefix with 'R:' to indicate response
                }
            } 
            else if (message.StartsWith("E:")) {
                // Handle exit command
                Console.WriteLine("Exit command received");
                return Encoding.UTF8.GetBytes("R:{\"Success\":true,\"Id\":\"\",\"Message\":\"Server acknowledges exit command\"}");
            }
            
            // Return a default error response if command processing failed
            return Encoding.UTF8.GetBytes("R:{\"Success\":false,\"Id\":\"\",\"Message\":\"Invalid command format or no handler available\"}");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error processing command: {ex.Message}");
            return Encoding.UTF8.GetBytes($"R:{{\"Success\":false,\"Id\":\"\",\"Message\":\"Error processing command: {ex.Message}\"}}");
        }
    }

    public Task StopAsync() {
        throw new NotImplementedException();
    }

    public Task SendBroadcastAsync(BroadcastMessage msg) {
        throw new NotImplementedException();
    }

    public Task SendBroadcastAsync(BroadcastMessage msg, CancellationToken token = default) {
        throw new NotImplementedException();
    }
}
