namespace McComms.gRPC;

/// <summary>
/// Implementation of ICommsServer based on gRPC.
/// This class adapts the functionality of GrpcServer to the standard ICommsServer interface.
/// In case you needed it, you can use ChannelOptions to add custom behavior to the gRPC server.
/// </summary>
public class CommsServerGrpc : ICommsServer
{
    private readonly GrpcServer _grpcServer;

    /// <summary>
    /// List of channel options for the gRPC server
    /// </summary>
    public List<ChannelOption> ChannelOptions { get; } = [];

    public ServerCredentials Credentials { get; set; } = ServerCredentials.Insecure;

    /// <summary>
    /// Default constructor that initializes the server with default settings
    /// </summary>
    public CommsServerGrpc() {
        _grpcServer = new GrpcServer(credentials: Credentials, channelOptions: ChannelOptions);
    }

    /// <summary>
    /// Constructor that allows specifying host and port for the server
    /// </summary>
    /// <param name="host">Address where the server will listen</param>
    /// <param name="port">Port where the server will listen</param>
    public CommsServerGrpc(string host, int port) {
        _grpcServer = new GrpcServer(host: host, port: port, credentials: Credentials, channelOptions: ChannelOptions);
    }

    /// <summary>
    /// Gets the underlying gRPC server instance
    /// </summary>
    public CommsHost CommsHost => _grpcServer.CommsHost;

    /// <summary>
    /// Callback that is invoked when a command is received from a client
    /// </summary>
    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    /// <summary>
    /// Starts the gRPC server and sets up callbacks to process commands
    /// </summary>
    /// <param name="onCommandReceived">Function that will process received commands</param>
    /// <param name="stoppingToken">Cancellation token to stop the server</param>
    public void Start(Func<CommandRequest, CommandResponse>? onCommandReceived, CancellationToken stoppingToken) {
        CommandReceived = onCommandReceived;
        _grpcServer.Start(OnCommandReceived);
    }

    /// <summary>
    /// Stops the gRPC server
    /// </summary>
    /// <remarks></remarks>
    public void Stop() {
        _grpcServer.Stop();
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients
    /// </summary>
    /// <param name="msg">The message to send to all clients</param>
    public void SendBroadcast(BroadcastMessage msg) {
        _grpcServer.SendBroadcast(new mcBroadcast { Id = msg.Id, Content = msg.Message });
    }

    /// <summary>
    /// Asynchronously sends a broadcast message to all connected clients
    /// </summary>
    /// <param name="msg">The message to send to all clients</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown if the gRPC server is not initialized</exception>
    public async Task SendBroadcastAsync(BroadcastMessage msg, CancellationToken cancellationToken = default) {
        await _grpcServer.SendBroadcastAsync(new mcBroadcast { Id = msg.Id, Content = msg.Message }, cancellationToken);
    }

    /// <summary>
    /// Processes a command received from a client and adapts it to the standard format
    /// </summary>
    /// <param name="request">The command in gRPC format</param>
    /// <returns>The response in gRPC format</returns>
    /// <exception cref="ArgumentNullException">Thrown if CommandReceived is not initialized</exception>
    private mcCommandResponse OnCommandReceived(mcCommandRequest request) {
        ArgumentNullException.ThrowIfNull(CommandReceived);
        var result = CommandReceived.Invoke(new CommandRequest(request.Id, request.Content));
        return new mcCommandResponse { Success = result.Success, Id = result.Id, Message = result.Message };
    }
}
