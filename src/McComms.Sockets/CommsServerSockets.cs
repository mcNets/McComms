namespace McComms.Sockets;

/// <summary>
/// Socket implementation of the ICommsServer interface.
/// Provides server-side functionality for socket-based communications with dual channels.
/// Uses one channel for commands and another for broadcasts to avoid blocking.
/// </summary>
public class CommsServerSockets : ICommsServer {
    private readonly SocketsServer _commandServer;
    private readonly SocketsServer _broadcastServer;

    /// <summary>
    /// Initializes a new instance of the CommsServerSockets class with default host and port.
    /// Creates dual channels: commands on default port, broadcasts on default port + 1.
    /// </summary>
    public CommsServerSockets() {
        var defaultAddress = IPAddress.Parse(SocketsServer.DEFAULT_HOST);
        _commandServer = new SocketsServer(defaultAddress, SocketsServer.DEFAULT_PORT);
        _broadcastServer = new SocketsServer(defaultAddress, SocketsServer.DEFAULT_PORT + 1);
    }

    /// <summary>
    /// Initializes a new instance of the CommsServerSockets class with specified host and port.
    /// Creates dual channels: commands on specified port, broadcasts on specified port + 1.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use for the command server.</param>
    public CommsServerSockets(IPAddress ipAddress, int port) {
        _commandServer = new SocketsServer(ipAddress, port);
        _broadcastServer = new SocketsServer(ipAddress, port + 1);
    }

    /// <summary>
    /// Gets the communication host information for the command server
    /// </summary>
    public CommsHost CommsHost => _commandServer.CommsHost;

    /// <summary>
    /// Callback function that is invoked when a command is received.
    /// </summary>
    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    /// <summary>
    /// Starts the server and begins listening for incoming connections and commands on both channels.
    /// </summary>
    /// <param name="commandReceived">Callback function invoked when a command is received.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    public void Start(Func<CommandRequest, CommandResponse>? commandReceived, CancellationToken stopToken) {
        CommandReceived = commandReceived;

        // Start command server on main port
        _ = Task.Run(async () => await _commandServer.ListenAsync(OnCommandReceived, stopToken), stopToken);

        // Start broadcast server on main port + 1 (no command processing needed)
        _ = Task.Run(async () => await _broadcastServer.ListenAsync(_ => [], stopToken), stopToken);
    }

    /// <summary>
    /// Stops the server and releases resources for both channels.
    /// </summary>
    public void Stop() {
        _commandServer.Dispose();
        _broadcastServer.Dispose();
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients via the broadcast channel.
    /// </summary>
    /// <param name="msg">The broadcast message to send.</param>
    public void SendBroadcast(BroadcastMessage msg) {
        _ = _broadcastServer.SendBroadcastAsync(SocketsHelper.Encode(msg.ToString()));
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients via the broadcast channel.
    /// </summary>
    /// <param name="msg">The broadcast message to send.</param>
    public async Task SendBroadcastAsync(BroadcastMessage msg) {
        await _broadcastServer.SendBroadcastAsync(SocketsHelper.Encode(msg.ToString()));
    }

    /// <summary>
    /// Processes received command messages, invokes the command handler, and returns the response.
    /// </summary>
    /// <param name="response">The received message as a byte array.</param>
    /// <returns>A byte array containing the encoded response.</returns>
    private byte[] OnCommandReceived(byte[] response) {
        var cmd = SocketsHelper.Decode(response);
        if (cmd.TryParseCommandRequest(out var request) == false || request == null) {
            return SocketsHelper.Encode(MsgHelper.Fail().ToString());
        }
        var result = CommandReceived?.Invoke(request);
        var encoded = result?.ToString() ?? MsgHelper.Fail().ToString();
        return SocketsHelper.Encode(encoded);
    }
}