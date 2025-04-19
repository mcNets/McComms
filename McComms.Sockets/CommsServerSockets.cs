namespace McComms.Sockets;

/// <summary>
/// Socket implementation of the ICommsServer interface.
/// Provides server-side functionality for socket-based communications.
/// </summary>
public class CommsServerSockets : ICommsServer {
    private readonly SocketsServer _socketServer;

    /// <summary>
    /// Initializes a new instance of the CommsServerSockets class with default host and port.
    /// </summary>
    public CommsServerSockets() {
        _socketServer = new SocketsServer();
    }

    /// <summary>
    /// Initializes a new instance of the CommsServerSockets class with specified host and port.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use for the server.</param>
    public CommsServerSockets(IPAddress ipAddress, int port) {
        _socketServer = new SocketsServer(ipAddress, port);
    }

    /// <summary>
    /// Gets or sets the callback function that is invoked when a command is received.
    /// </summary>
    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    /// <summary>
    /// Starts the server and begins listening for incoming connections and commands.
    /// </summary>
    /// <param name="commandReceived">Callback function invoked when a command is received.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    public void Start(Func<CommandRequest, CommandResponse>? commandReceived, CancellationToken stopToken) {
        CommandReceived = commandReceived;
        _ = Task.Run(async () => await _socketServer.ListenAsync(OnCommandReceived, stopToken), stopToken);
    }

    /// <summary>
    /// Stops the server and releases resources.
    /// </summary>
    public void Stop() { }

    /// <summary>
    /// Sends a broadcast message to all connected clients.
    /// </summary>
    /// <param name="msg">The broadcast message to send.</param>
    public void SendBroadcast(BroadcastMessage msg) {
        // Fire and forget - SendBroadcast does not catch exceptions.
        _ = _socketServer.SendBroadcastAsync(SocketsHelper.Encode(msg.ToString()));
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