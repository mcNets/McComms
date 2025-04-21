namespace McComms.gRPC;

using System.Diagnostics;

/// <summary>
/// gRPC server that implements the service defined in the proto file.
/// This class manages client connections and message distribution.
/// </summary>
public class GrpcServer : mcServeis.mcServeisBase
{
    // Collection of stream writers to send broadcast messages to all connected clients
    private readonly List<IServerStreamWriter<mcBroadcast>> _broadcastWriters;
    
    // The gRPC server that handles connections
    private readonly Server? _server;

    // The host address where the server listens for incoming connections
    private readonly string _host;

    // The port on which the server listens for incoming connections
    private readonly int _port;

    // Flag indicating whether the server is currently running
    private bool _isRunning = false;

    // Default host and port for the server
    public const string DEFAULT_HOST = "127.0.0.1";
    public const int DEFAULT_PORT = 50001;

    // Callback that is invoked when a command is received from a client
    private Func<mcCommandRequest, mcCommandResponse>? OnCommandReceived { get; set; }

    /// <summary>
    /// Default constructor that initializes the server on the default host and port
    /// </summary>
    public GrpcServer() {
        _broadcastWriters = new();
        _port = DEFAULT_PORT;
        _host = DEFAULT_HOST;
        _server = new Server {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };
    }

    /// <summary>
    /// Constructor that allows specifying host and port for the server
    /// </summary>
    /// <param name="host">Address where the server will listen</param>
    /// <param name="port">Port where the server will listen</param>
    public GrpcServer(string host, int port) {
        _broadcastWriters = [];
        _port = port;
        _host = host;
        _server = new Server {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };
    }

    public Server? Server => _server;
    public string Host => _host;
    public int Port => _port;

    /// <summary>
    /// Starts the gRPC server and configures the callback for received commands
    /// </summary>
    /// <param name="onCommandReceived">Function that will process received commands</param>
    /// <exception cref="ArgumentNullException">Thrown if the server is not initialized</exception>
    public void Start(Func<mcCommandRequest, mcCommandResponse>? onCommandReceived) {
        ArgumentNullException.ThrowIfNull(_server);
        this.OnCommandReceived = onCommandReceived;
        _server.Start();
        _isRunning = true;
    }

    /// <summary>
    /// Stops the gRPC server and releases all resources
    /// </summary>
    public void Stop()
    {
        if (_server != null && _isRunning) {
            _server.ShutdownAsync().Wait();
            _isRunning = false;
        }
        
        _broadcastWriters.Clear();
    }

    /// <summary>
    /// Overrides the gRPC service method to process commands sent by clients
    /// </summary>
    /// <param name="request">The command received from the client</param>
    /// <param name="context">The gRPC call context</param>
    /// <returns>A response to the command</returns>
    /// <exception cref="ArgumentNullException">Thrown if the callback to process commands is not configured</exception>
    public override Task<mcCommandResponse> SendCommand(mcCommandRequest request, ServerCallContext context) {
        ArgumentNullException.ThrowIfNull(OnCommandReceived);
        var response = OnCommandReceived.Invoke(new mcCommandRequest { Id = request.Id, Content = request.Content });
        return Task.FromResult(new mcCommandResponse { Succes = response.Succes, Id = response.Id, Message = response.Message });
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients
    /// </summary>
    /// <param name="message">The message to send to all clients</param>
    public void SendBroadcast(mcBroadcast message) {
        // Check if the server is running before sending a broadcast
        if (_server == null || !_isRunning) {
            Debug.WriteLine("Server is not running. Cannot send broadcast.");
            throw new InvalidOperationException("Server is not running. Cannot send broadcast.");
        }

        // Executed as a background task to avoid blocking the main thread
        Task.Run(async () => {
            foreach (var writer in _broadcastWriters) {
                try {
                    await writer.WriteAsync(message);
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error sending broadcast: {ex.Message}");
                    // We don't propagate the exception to allow other writers to receive the message
                }
            }
        });
    }

    /// <summary>
    /// Implements the gRPC service method to manage streaming connections
    /// This method keeps connections with clients open to send broadcasts
    /// </summary>
    /// <param name="requestStream">Input stream (not used, but required by gRPC)</param>
    /// <param name="responseStream">Stream for sending messages to the client</param>
    /// <param name="context">The gRPC call context</param>
    public override async Task Broadcast(IAsyncStreamReader<Empty> requestStream, IServerStreamWriter<mcBroadcast> responseStream, ServerCallContext context) {
        // Adds the client to the list of broadcast receivers
        lock (_broadcastWriters) {
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
            lock (_broadcastWriters) {
                _broadcastWriters.Remove(responseStream);
                Debug.WriteLine("Client disconnected.");
            }
        }
    }
}
