namespace McComms.gRPC;

using System.Diagnostics;

/// <summary>
/// gRPC server that implements the service defined in the proto file.
/// This class manages client connections and message distribution.
/// </summary>
public sealed class GrpcServer : mcServeis.mcServeisBase {
    private readonly Server? _server;
    private readonly NetworkAddress _address = new(DefaultNetworkSettings.DEFAULT_HOST, DefaultNetworkSettings.DEFAULT_PORT);
    private readonly List<IServerStreamWriter<mcBroadcast>> _broadcastWriters = [];
    private readonly Lock _broadcastWritersLock = new();

    private Func<mcCommandRequest, mcCommandResponse>? OnCommandReceived { get; set; }

    /// <summary>
    /// Gets the underlying gRPC server instance
    /// </summary>
    public Server? Server => _server ?? throw new InvalidOperationException("Server is not initialized.");

    /// <summary>
    /// Indicates whether the server is currently running
    /// </summary>
    public bool IsRunning { get; private set; } = false;

    /// <summary>
    /// Gets the NetworkAddress object that contains the host and port information
    /// </summary>
    public NetworkAddress Address => _address;

    /// <summary>
    /// Default constructor that initializes the server on the default host and port
    /// </summary>
    public GrpcServer(ServerCredentials credentials, IEnumerable<ChannelOption>? channelOptions = null) {
        _server = new Server(channelOptions) {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort(Address.Host, Address.Port, credentials) }
        };
    }

    /// <summary>
    /// Constructor that allows specifying host and port for the server
    /// </summary>
    /// <param name="address">The NetworkAddress object containing the host and port information</param>
    public GrpcServer(NetworkAddress address, ServerCredentials credentials, IEnumerable<ChannelOption>? channelOptions = null) {
        _address = address;
        _server = new Server(channelOptions) {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort(Address.Host, Address.Port, credentials) }
        };
    }

    /// <summary>
    /// Starts the gRPC server and configures the callback for received commands
    /// </summary>
    /// <param name="onCommandReceived">Function that will process received commands</param>
    /// <exception cref="ArgumentNullException">Thrown if the server is not initialized</exception>
    public bool Start(Func<mcCommandRequest, mcCommandResponse> onCommandReceived) {
        if (_server == null) {
            throw new InvalidOperationException("gRPC server is not initialized.");
        }

        if (IsRunning) {
            Debug.WriteLine("Server is already running.");
            return true;
        }

        OnCommandReceived = onCommandReceived;

        try {
            IsRunning = false;
            _server.Start();
            IsRunning = true;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error starting server: {ex.Message}");
            return false;
        }

        return IsRunning;
    }

    /// <summary>
    /// Stops the gRPC server.
    /// </summary>
    public void Stop() {
        _ = StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the gRPC server.
    /// </summary>
    public async Task StopAsync() {
        if (_server != null && IsRunning) {
            try {
                await _server.ShutdownAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error shutting down server: {ex.Message}");
            }
        }

        IsRunning = false;
        using (_broadcastWritersLock.EnterScope()) {
            _broadcastWriters.Clear();
        }
    }

    /// <summary>
    /// Overrides the gRPC service method to process commands sent by clients
    /// </summary>
    /// <param name="request">The command received from the client</param>
    /// <param name="context">The gRPC call context</param>
    /// <returns>A response to the command</returns>
    /// <exception cref="ArgumentNullException">Thrown if the callback to process commands is not configured</exception>
    public override Task<mcCommandResponse> SendCommand(mcCommandRequest request, ServerCallContext context) {
        if (OnCommandReceived == null) {
            throw new InvalidOperationException("Command handler is not configured.");
        }

        try {
            var response = OnCommandReceived.Invoke(new mcCommandRequest { Id = request.Id, Content = request.Content });
            return Task.FromResult(new mcCommandResponse { Success = response.Success, Id = response.Id, Message = response.Message });
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error processing command {request.Id}: {ex.Message}");
            return Task.FromResult(new mcCommandResponse { Success = false, Id = request.Id.ToString(), Message = $"Error processing command: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients
    /// </summary>
    /// <param name="message">The message to send to all clients</param>
    public void SendBroadcast(mcBroadcast message) {
        if (_server == null || !IsRunning) {
            Debug.WriteLine("Server is not running. Cannot send broadcast.");
            throw new InvalidOperationException("Server is not running. Cannot send broadcast.");
        }

        Task.Run(async () => {
            await SendBroadcastAsync(message);
        });
    }

    /// <summary>
    /// Asynchronously sends a broadcast message to all connected clients   
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SendBroadcastAsync(mcBroadcast message, CancellationToken cancellationToken = default) {
        if (_server == null || !IsRunning) {
            Debug.WriteLine("Server is not running. Cannot send broadcast.");
            throw new InvalidOperationException("Server is not running. Cannot send broadcast.");
        }

        IServerStreamWriter<mcBroadcast>[] writers;
        using (_broadcastWritersLock.EnterScope()) {
            writers = [.. _broadcastWriters];
        }

        foreach (var writer in writers) {
            try {
                await writer.WriteAsync(message, cancellationToken);
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error sending broadcast: {ex.Message}");
                // We don't propagate the exception to allow other writers to receive the message
            }
        }
    }

    /// <summary>
    /// Implements the gRPC service method to manage streaming connections.
    /// This method keeps opened connections with clients to send broadcasts.
    /// </summary>
    /// <param name="requestStream">Input stream (not used, but required by gRPC)</param>
    /// <param name="responseStream">Stream for sending messages to the client</param>
    /// <param name="context">The gRPC call context</param>
    public override async Task Broadcast(IAsyncStreamReader<Empty> requestStream, IServerStreamWriter<mcBroadcast> responseStream, ServerCallContext context) {
        // Adds the client to the list of broadcast receivers
        using (_broadcastWritersLock.EnterScope()) {
            _broadcastWriters.Add(responseStream);
            Debug.WriteLine("Client connected.");
        }

        // Keeps the connection open until the client disconnects
        try {
            while (!context.CancellationToken.IsCancellationRequested) {
                await Task.Delay(100, context.CancellationToken); // Increased to reduce CPU usage
            }
        }
        catch (TaskCanceledException) {
            // Expected when the client disconnects
        }
        finally {
            // Cleans up resources when the client disconnects
            using (_broadcastWritersLock.EnterScope()) {
                _broadcastWriters.Remove(responseStream);
                Debug.WriteLine("Client disconnected.");
            }
        }
    }
}
