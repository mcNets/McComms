namespace McComms.Sockets;

/// <summary>
/// Socket implementation of the ICommsClient interface.
/// Provides client-side functionality for socket-based communications with dual channels.
/// Uses one channel for commands and another for broadcasts to avoid blocking.
/// </summary>
public class CommsClientSockets : ICommsClient {
    private readonly SocketsClient _commandClient;
    private readonly SocketsClient _broadcastClient;

    /// <summary>
    /// Initializes a new instance of the CommsClientSockets class with default host and port.
    /// Creates dual channels: commands on default port, broadcasts on default port + 1.
    /// </summary>
    public CommsClientSockets() {
        var defaultAddress = IPAddress.Parse(SocketsClient.DEFAULT_HOST);
        _commandClient = new SocketsClient(defaultAddress, SocketsClient.DEFAULT_PORT);
        _broadcastClient = new SocketsClient(defaultAddress, SocketsClient.DEFAULT_PORT + 1);
    }

    /// <summary>
    /// Initializes a new instance of the CommsClientSockets class with specified host and port.
    /// Creates dual channels: commands on specified port, broadcasts on specified port + 1.
    /// </summary>
    /// <param name="host">The IP address of the host to connect to.</param>
    /// <param name="port">The port number to use for the command connection.</param>
    public CommsClientSockets(IPAddress host, int port) {
        _commandClient = new SocketsClient(host, port);
        _broadcastClient = new SocketsClient(host, port + 1);
    }

    /// <summary>
    /// Gets the communication host information for the command client
    /// </summary>
    public CommsHost CommsHost => _commandClient.CommsHost;

    /// <summary>
    /// Gets or sets the callback action that is invoked when a broadcast message is received.
    /// </summary>
    public Action<BroadcastMessage>? OnBroadcastReceived { get; set; }

    /// <summary>
    /// Connects to the server on both channels and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>True if connection is successful on both channels, false otherwise.</returns>
    public bool Connect(Action<BroadcastMessage>? onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        
        // Connect command client (no broadcast callback needed)
        var commandConnected = _commandClient.Connect(_ => { });
        
        // Connect broadcast client with broadcast callback
        var broadcastConnected = _broadcastClient.Connect(BroadcastReceived);
        
        return commandConnected && broadcastConnected;
    }

    /// <summary>
    /// Asynchronously connects to the server on both channels and sets the callback for broadcast messages.
    /// </summary>
    /// <param name="onBroadcastReceived">Callback invoked when a broadcast message is received.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter is true if connection is successful on both channels, false otherwise.</returns>
    public async Task<bool> ConnectAsync(Action<BroadcastMessage>? onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        
        // Connect command client (no broadcast callback needed)
        var commandConnected = await _commandClient.ConnectAsync(_ => { });
        
        // Connect broadcast client with broadcast callback
        var broadcastConnected = await _broadcastClient.ConnectAsync(BroadcastReceived);
        
        return commandConnected && broadcastConnected;
    }

    /// <summary>
    /// Disconnects from the server on both channels after sending an exit command.
    /// </summary>
    public void Disconnect() {
        if (_commandClient.IsConnected) {
            SendExitCommand();
        }
        _commandClient.Dispose();
        _broadcastClient.Dispose();
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
    /// Sends a command to the server via the command channel and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <returns>A CommandResponse object with the server's response.</returns>
    public CommandResponse SendCommand(CommandRequest msg) {
        var result = _commandClient.Send(SocketsHelper.Encode(msg.ToString()));
        if (result == SocketsHelper.NAK_MSG) {
            return MsgHelper.Fail("SCK001", "NAK received");
        }

        var decoded = SocketsHelper.Decode(result);
        if (decoded.TryParseCommandResponse(out var response) == false || response == null) {
            return MsgHelper.Fail("SCK002", "Invalid response");
        }
        return response;
    }

    /// <summary>
    /// Asynchronously sends a command to the server via the command channel and returns the response.
    /// </summary>
    /// <param name="msg">The command request to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains a CommandResponse object with the server's response.</returns>
    public async Task<CommandResponse> SendCommandAsync(CommandRequest msg, CancellationToken cancellationToken = default) {
        var result = await _commandClient.SendAsync(SocketsHelper.Encode(msg.ToString()), cancellationToken);
        if (result == SocketsHelper.NAK_MSG) {
            return MsgHelper.Fail("SCK001", "NAK received");
        }

        var decoded = SocketsHelper.Decode(result);
        if (decoded.TryParseCommandResponse(out var response) == false || response == null) {
            return MsgHelper.Fail("SCK002", "Invalid response");
        }
        return response;
    }

    /// <summary>
    /// Sends an exit command to the server via the command channel.
    /// </summary>
    public void SendExitCommand() {
        _commandClient.Send(SocketsHelper.EOT_MSG);
    }

    /// <summary>
    /// Asynchronously sends an exit command to the server via the command channel.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendExitCommandAsync(CancellationToken cancellationToken = default) {
        await _commandClient.SendAsync(SocketsHelper.EOT_MSG, cancellationToken);
    }
}