using System.Diagnostics;

namespace McComms.gRPC;

/// <summary>
/// gRPC client that connects to the server and manages communications.
/// This class handles both sending commands and receiving broadcast messages.
/// </summary>
public class GrpcClient : IDisposable, IAsyncDisposable {
    // Default host and port
    public const string DEFAULT_HOST = "0.0.0.0";
    public const int DEFAULT_PORT = 50051;

    // Communication channel with the gRPC server
    private readonly Channel? _channel;

    // gRPC client generated from the proto file
    private readonly mcServeis.mcServeisClient _client;

    // Cancellation token to manage client disconnection
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // Task that handles broadcast message listening
    private Task? _broadcastTask;

    // Tracking whether Dispose has been called
    private bool _disposed = false;

    private readonly CommsHost? _commsHost;

    /// <summary>
    /// Default constructor that initializes the connection to localhost:50051
    /// </summary>
    public GrpcClient() {
        _commsHost = new CommsHost(DEFAULT_HOST, DEFAULT_PORT);
        _channel = new Channel(_commsHost.Host, _commsHost.Port, ChannelCredentials.Insecure);
        _client = new mcServeis.mcServeisClient(_channel);
    }

    /// <summary>
    /// Constructor that allows specifying host and port for the connection
    /// </summary>
    /// <param name="host">gRPC server address</param>
    /// <param name="port">gRPC server port</param>
    public GrpcClient(string host, int port) {
        _commsHost = new CommsHost(host, port);
        _channel = new Channel(_commsHost.Host, _commsHost.Port, ChannelCredentials.Insecure);
        _client = new mcServeis.mcServeisClient(_channel);
    }

    /// <summary>
    /// Gets the CommsHost object that contains the host and port information
    /// </summary>
    public CommsHost CommsHost => _commsHost ?? throw new InvalidOperationException("CommsHost is not initialized. Please ensure the client is properly constructed.");

    /// <summary>
    /// Gets the gRPC channel used for communication with the server
    /// </summary>
    public mcServeis.mcServeisClient Client => _client;

    /// <summary>
    /// Callback that is invoked when a broadcast message is received
    /// </summary>
    public Action<mcBroadcast>? OnBroadcastReceived { get; set; }

    /// <summary>
    /// Connects to the gRPC server and sets up broadcast listening
    /// </summary>
    /// <param name="onBroadcastReceived">Callback function that will be executed when a broadcast is received</param>
    /// <returns>true if the connection is started successfully</returns>
    public bool Connect(Action<mcBroadcast> onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        var token = _cancellationTokenSource.Token;

        try {
            _broadcastTask = Task.Run(async () => {
                var broadcastCall = _client.Broadcast(cancellationToken: token);

                while (await broadcastCall.ResponseStream.MoveNext(token)) {
                    try {
                        var broadcastMessage = broadcastCall.ResponseStream.Current;
                        OnBroadcastReceived?.Invoke(broadcastMessage);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Error processing broadcast: {ex.Message}");
                        // Not throwing the exception to keep the stream active
                    }
                }
            }, token);
            return true;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error connecting: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Connects to the gRPC server and sets up broadcast listening asynchronously
    /// </summary>
    /// <param name="onBroadcastReceived">Callback function that will be executed when a broadcast is received</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The value is true if the connection is started successfully</returns>
    public async Task<bool> ConnectAsync(Action<mcBroadcast> onBroadcastReceived, CancellationToken cancellationToken = default) {
        OnBroadcastReceived = onBroadcastReceived;
        var token = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken).Token;

        try {
            _broadcastTask = Task.Run(async () => {
                var broadcastCall = _client.Broadcast(cancellationToken: token);

                while (await broadcastCall.ResponseStream.MoveNext(token)) {
                    try {
                        var broadcastMessage = broadcastCall.ResponseStream.Current;
                        OnBroadcastReceived?.Invoke(broadcastMessage);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Error processing broadcast: {ex.Message}");
                        // Not throwing the exception to keep the stream active
                    }
                }
            }, token);

            // Wait for the task to be properly started
            await Task.Delay(100, cancellationToken);
            return true;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error connecting asynchronously: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Safely disconnects from the gRPC server
    /// </summary>
    public void Disconnect() {
        Task.Run( async () => {
            try {
                await DisconnectAsync();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error during disconnection: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Safely disconnects from the gRPC server asynchronously
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation</returns>
    public async Task DisconnectAsync() {
        try {
            _cancellationTokenSource?.Cancel();

            if (_channel != null) {
                await _channel.ShutdownAsync();
            }

            if (_broadcastTask != null) {
                try {
                    await _broadcastTask;
                }
                catch (OperationCanceledException) {
                    // Expected during cancellation
                }
            }
        }
        catch (Exception ex) {
            // Errors during disconnection can generally be ignored
            Debug.WriteLine($"Error during disconnection: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a command to the server and waits for its response
    /// </summary>
    /// <param name="msg">The command to send</param>
    /// <returns>The server's response</returns>
    public mcCommandResponse SendCommand(mcCommandRequest msg) {
        // Configure a reasonable timeout for the operation
        CallOptions options = new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddSeconds(3));

        try {
            return _client.SendCommand(msg, options: options);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error sending command: {ex.Message}");
            // Return an error message if communication fails
            return new mcCommandResponse {
                Success = false,
                Id = msg.Id.ToString(),
                Message = $"Communication error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sends a command to the server and waits for its response asynchronously
    /// </summary>
    /// <param name="msg">The command to send</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The value contains the server's response</returns>
    public async Task<mcCommandResponse> SendCommandAsync(mcCommandRequest msg, CancellationToken cancellationToken = default) {
        // Configure a reasonable timeout for the operation
        CallOptions options = new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddSeconds(3))
            .WithCancellationToken(cancellationToken);

        try {
            return await _client.SendCommandAsync(msg, options);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error sending command asynchronously: {ex.Message}");
            // Return an error message if communication fails
            return new mcCommandResponse {
                Success = false,
                Id = msg.Id.ToString(),
                Message = $"Communication error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Releases all resources used by the GrpcClient
    /// </summary>
    public void Dispose() {
        if (_disposed) return;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the GrpcClient and optionally releases the managed resources
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                // Dispose managed resources
                Disconnect();
                _cancellationTokenSource?.Dispose();
            }
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Asynchronously releases the resources used by the GrpcClient
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (!_disposed) {
            await DisconnectAsync().ConfigureAwait(false);
            _cancellationTokenSource?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
