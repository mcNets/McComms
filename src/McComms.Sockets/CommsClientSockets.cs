namespace McComms.Sockets;

/// <summary>
/// Socket implementation of the ICommsClient interface.
/// Provides client-side functionality for socket-based communications.
/// </summary>
public class CommsClientSockets : ICommsClient, IDisposable {
    private readonly SocketsClient _client;

    /// <summary>
    /// Gets the network address of the connected server.
    /// </summary>
    public NetworkAddress Address => _client.Address;

    /// <summary>
    /// Gets the communication protocol used by this client.
    /// </summary>
    public CommsProtocol Protocol => CommsProtocol.Sockets;

    /// <summary>
    /// Initializes a new instance of the CommsClientSockets class with default host and port.
    /// </summary>
    public CommsClientSockets() {
        _client = new SocketsClient();
    }

    /// <summary>
    /// Initializes a new instance of the CommsClientSockets class with specified host and port.
    /// </summary>
    /// <param name="address">The NetworkAddress to connect to.</param>
    public CommsClientSockets(NetworkAddress address) {
        _client = new SocketsClient(address);
    }

    /// <summary>
    /// Initializes a new instance of the CommsClientSockets class with specified host, port, and timeout settings.
    /// </summary>
    /// <param name="address">The NetworkAddress to connect to.</param>
    /// <param name="readTimeoutMs">Read timeout in milliseconds.</param>
    public CommsClientSockets(NetworkAddress address, int readTimeoutMs) {
        _client = new SocketsClient(address, readTimeoutMs: readTimeoutMs);
    }

    /// <summary>
    /// Gets or sets the callback action that is invoked when a broadcast message is received.
    /// </summary>
    public Action<BroadcastMessage>? OnBroadcastReceived { get; set; }

    /// <summary>
    /// Connects to the server and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>True if connection is successful, false otherwise.</returns>
    public bool Connect(Action<BroadcastMessage>? onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        return _client.Connect(BroadcastReceived);
    }

    /// <summary>
    /// Asynchronously connects to the server and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter is true if connection is successful, false otherwise.</returns>
    public async Task<bool> ConnectAsync(Action<BroadcastMessage>? onBroadcastReceived, CancellationToken token = default) {
        OnBroadcastReceived = onBroadcastReceived;
        return await _client.ConnectAsync(BroadcastReceived, token);
    }

    /// <summary>
    /// Disconnects from the server after sending an exit command.
    /// </summary>
    public void Disconnect() {
        if (_client.IsConnected){
            SendExitCommand();
        }
        
        // Clear event handlers to prevent memory leaks
        OnBroadcastReceived = null;
        
        _client.Dispose();
    }

    public Task DisconnectAsync() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handles received broadcast messages, parses them and invokes the broadcast callback.
    /// </summary>
    /// <param name="message">The received message as a byte array.</param>
    public void BroadcastReceived(ReadOnlySpan<byte> message) {
        if (SocketsHelper.Decode(message).TryParseBroadcastMessage(out var command) && command != null) {
            OnBroadcastReceived?.Invoke(command);
        }
    }

    /// <summary>
    /// Sends a command to the server and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <returns>A CommandResponse object with the server's response.</returns>
    public CommandResponse SendCommand(CommandRequest msg) {
        var result = _client.Send(SocketsHelper.Encode(msg.ToString()));
        if (result == SocketsHelper.NAK_MSG) {
            return new CommandResponse(false, "SCK001", "NAK received");
        }

        var decoded = SocketsHelper.Decode(result);
        if (decoded.TryParseCommandResponse(out var response) == false || response == null) {
            return new CommandResponse(false, "SCK002", "Invalid response");
        }
        return response;
    }

    /// <summary>
    /// Asynchronously sends a command to the server and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains a CommandResponse object with the server's response.</returns>
    public async Task<CommandResponse> SendCommandAsync(CommandRequest msg, CancellationToken cancellationToken = default) {
        var result = await _client.SendAsync(SocketsHelper.Encode(msg.ToString()), cancellationToken);
        if (result == SocketsHelper.NAK_MSG) {
            return new CommandResponse(false, "SCK001", "NAK received");
        }

        var decoded = SocketsHelper.Decode(result);
        if (decoded.TryParseCommandResponse(out var response) == false || response == null) {
            return new CommandResponse(false, "SCK002", "Invalid response");
        }
        return response;
    }

    /// <summary>
    /// Sends an exit command to the server.
    /// </summary>
    public void SendExitCommand() {
        _client.Send(SocketsHelper.EOT_MSG);
    }

    /// <summary>
    /// Asynchronously sends an exit command to the server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendExitCommandAsync(CancellationToken cancellationToken = default) {
        await _client.SendAsync(SocketsHelper.EOT_MSG, cancellationToken);
    }

    /// <summary>
    /// Disposes the client and clears event handlers.
    /// </summary>
    public void Dispose() {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}