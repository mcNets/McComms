using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace McComms.Sockets;

/// <summary>
/// Provides socket server functionality for handling client connections, 
/// message processing, and broadcasting using two separate ports.
/// Uses main port for commands and main port + 1 for broadcasts.
/// </summary>
public sealed class SocketsServer : IDisposable {
    /// Constants
    public const int DEFAULT_BUFFER_SIZE = 1500;
    public const int DEFAULT_POLL_DELAY_MS = 5;

    /// TCP listener socket for accepting incoming command client connections.
    private readonly Socket? _commandListener;

    /// TCP listener socket for accepting incoming broadcast client connections (port + 1).
    private readonly Socket? _broadcastListener;

    /// Endpoint for the command TCP listener defining the IP address and port to listen on.
    private readonly IPEndPoint? _commandEndPoint = null;

    /// Endpoint for the broadcast TCP listener (port + 1).
    private readonly IPEndPoint? _broadcastEndPoint = null;

    /// Thread-safe collection of currently connected command clients.
    private readonly ConcurrentDictionary<int, SocketsClientModel> _commandClients = new();

    /// Thread-safe collection of currently connected broadcast clients.
    private readonly ConcurrentDictionary<int, SocketsClientModel> _broadcastClients = new();

    /// Counter for assigning unique client IDs.
    private int _nextClientId = 1;

    /// Callback function for handling received messages from clients.
    private Func<byte[], byte[]>? _onMessageReceived;

    /// Poll delay in milliseconds to avoid busy waiting.
    private readonly int _pollDelayMs;

    // Semaphore for message processing synchronization
    private readonly SemaphoreSlim _messageProcessingLock = new(1, 1);

    /// <summary>
    /// The communication host that defines the address and port for the server.
    /// </summary>
    private readonly NetworkAddress _address = new(DefaultNetworkSettings.DEFAULT_SERVER_HOST, DefaultNetworkSettings.DEFAULT_PORT);

    /// <summary>
    /// Gets the network address for the server.
    /// </summary>
    public NetworkAddress Address => _address;

    /// <summary>
    /// Initializes a new instance of the SocketsServer class with the specified NetworkAddress
    /// </summary>
    /// <param name="address">The NetworkAddress to listen on.</param>
    public SocketsServer() : this(new NetworkAddress(DefaultNetworkSettings.DEFAULT_SERVER_HOST, DefaultNetworkSettings.DEFAULT_PORT)) {
    }

    /// <summary>
    /// Initializes a new instance of the SocketsServer class with the specified IP address, port, and poll delay.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use for commands (broadcast will use port + 1).</param>
    /// <param name="pollDelayMs">The delay in milliseconds between polls when no data is available.</param>
    public SocketsServer(NetworkAddress address, int pollDelayMs = DEFAULT_POLL_DELAY_MS) {
        _address = address;
        _commandEndPoint = new IPEndPoint(IPAddress.Parse(address.Host), address.Port);
        _broadcastEndPoint = new IPEndPoint(IPAddress.Parse(address.Host), address.Port + 1);
        _commandListener = new Socket(_commandEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _broadcastListener = new Socket(_broadcastEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _pollDelayMs = pollDelayMs > 0 ? pollDelayMs : DEFAULT_POLL_DELAY_MS;
    }

    /// <summary>
    /// Starts listening for incoming client connections and processes their messages.
    /// </summary>
    /// <param name="onMessageReceived">Callback function invoked when a message is received from a client.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    public void Listen(Func<byte[], byte[]> onMessageReceived, CancellationToken stopToken = default) {
        // Call the async version and block until it completes
        // This maintains backward compatibility
        _ = Task.Run(async () => await ListenAsync(onMessageReceived, stopToken), stopToken);
    }

    /// <summary>
    /// Starts listening for incoming client connections and processes their messages asynchronously.
    /// This now handles both command and broadcast connections on separate ports to avoid conflicts.
    /// </summary>
    /// <param name="onMessageReceived">Callback function invoked when a message is received from a client.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ListenAsync(Func<byte[], byte[]> onMessageReceived, CancellationToken stopToken = default) {
        if (_commandListener == null || _broadcastListener == null || _commandEndPoint == null || _broadcastEndPoint == null) {
            throw new InvalidOperationException("Server not properly initialized.");
        }

        _onMessageReceived = onMessageReceived;

        _commandListener.Bind(_commandEndPoint!);
        _commandListener.Listen();
        _broadcastListener.Bind(_broadcastEndPoint!);
        _broadcastListener.Listen();

        var commandTask = Task.Run(async () => await AcceptCommandClientsAsync(stopToken), stopToken);
        var broadcastTask = Task.Run(async () => await AcceptBroadcastClientsAsync(stopToken), stopToken);

        try {
            await Task.WhenAny(commandTask, broadcastTask);
        }
        catch (OperationCanceledException) {
            // Expected when stopping the server
        }
    }

    /// <summary>
    /// Accepts incoming command client connections.
    /// </summary>
    private async Task AcceptCommandClientsAsync(CancellationToken stopToken) {
        if (_commandListener == null) {
            throw new InvalidOperationException("Command listener not initialized.");
        }

        while (!stopToken.IsCancellationRequested) {
            try {
                Socket client = await _commandListener.AcceptAsync(stopToken);
                var socketClient = new SocketsClientModel(client, new NetworkStream(client), DEFAULT_BUFFER_SIZE) {
                    Id = Interlocked.Increment(ref _nextClientId)
                };
                _commandClients.TryAdd(socketClient.Id, socketClient);

                Debug.WriteLine($"Command client connected, total command clients: {_commandClients.Count}");

                _ = Task.Run(async () => await CommandMessagesHandler(socketClient, stopToken), stopToken);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error accepting command client: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Accepts incoming broadcast client connections.
    /// </summary>
    private async Task AcceptBroadcastClientsAsync(CancellationToken stopToken) {
        if (_broadcastListener == null) {
            throw new InvalidOperationException("Broadcast listener not initialized.");
        }

        while (!stopToken.IsCancellationRequested) {
            try {
                Socket client = await _broadcastListener.AcceptAsync(stopToken);
                var socketClient = new SocketsClientModel(client, new NetworkStream(client), DEFAULT_BUFFER_SIZE) {
                    Id = Interlocked.Increment(ref _nextClientId)
                };
                _broadcastClients.TryAdd(socketClient.Id, socketClient);

                Debug.WriteLine($"Broadcast client connected, total broadcast clients: {_broadcastClients.Count}");

                // Broadcast clients only receive, no message handling needed
                // They will be used only for sending broadcast messages
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error accepting broadcast client: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a broadcast message to all connected broadcast clients asynchronously.
    /// </summary>
    /// <param name="command">The message to broadcast as a byte array.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async ValueTask SendBroadcastAsync(byte[] command, CancellationToken cancellationToken = default) {
        await _messageProcessingLock.WaitAsync(cancellationToken);
        try {
            int disconnectedCount = 0;
            var clientsToRemove = new List<SocketsClientModel>();

            foreach (var client in _broadcastClients.Values) {
                try {
                    if (client.Connected) {
                        await client.Stream.WriteAsync(command, cancellationToken);
                    }
                    else {
                        clientsToRemove.Add(client);
                    }
                }
                catch {
                    client.Connected = false;
                    clientsToRemove.Add(client);
                    disconnectedCount++;
                }
            }

            if (clientsToRemove.Count > 0) {
                foreach (var client in clientsToRemove) {
                    try {
                        client.Dispose();
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"Error disposing broadcast client: {ex.Message}");
                    }

                    // Remove from dictionary - O(1) operation
                    _broadcastClients.TryRemove(client.Id, out _);
                }

                Debug.WriteLine($"{clientsToRemove.Count} broadcast client(s) removed immediately, remaining clients: {_broadcastClients.Count}");
            }

            if (disconnectedCount > 0) {
                Debug.WriteLine($"{disconnectedCount} client(s) marked as disconnected and cleaned up");
            }
        }
        finally {
            _messageProcessingLock.Release();
        }
    }

    /// <summary>
    /// Handles command messages from a specific client, processing incoming data according to the protocol.
    /// </summary>
    /// <param name="client">The client model representing the connected client.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to stop message handling.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>    
    private async Task CommandMessagesHandler(SocketsClientModel client, CancellationToken cancellationToken) {

        // Get direct reference to message buffer
        var bufferMessage = client.MessageBuffer;

        // Use ArrayPool to rent a buffer instead of allocating a new one
        // More efficient than creating a new array for each client
        byte[] buffer = ArrayPool<byte>.Shared.Rent(DEFAULT_BUFFER_SIZE);

        bool receivingMessage = false;

        try {
            // Main message processing loop
            while (!cancellationToken.IsCancellationRequested && client.Connected) {
                if (client.Stream.DataAvailable) {
                    var bytesRead = await client.Stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead <= 0) {
                        continue;
                    }
                    for (var x = 0; x < bytesRead; x++) {
                        switch (buffer[x]) {
                            case SocketsHelper.EOT:
                                // Handle end of transmission: send ACK and disconnect
                                byte[] ack = SocketsHelper.Encode("0:0");
                                await client.Stream.WriteAsync(ack, cancellationToken);
                                client.Connected = false;
                                // Client will be removed in finally block
                                return;
                            case SocketsHelper.STX:
                                bufferMessage.Clear();
                                receivingMessage = true;
                                break;
                            case SocketsHelper.ETX:
                                if (receivingMessage && bufferMessage.Count > 0 && _onMessageReceived != null) {
                                    var message = bufferMessage.ToArray();
                                    var response = _onMessageReceived?.Invoke(message);
                                    if (response != null) {
                                        await client.Stream.WriteAsync(response, cancellationToken);
                                    }
                                }
                                receivingMessage = false;
                                bufferMessage.Clear();
                                break;
                            default:
                                if (receivingMessage) {
                                    bufferMessage.Add(buffer[x]);
                                }
                                break;
                        }
                    }
                }
                else {
                    // Avoid busy waiting using configurable delay                
                    await Task.Delay(_pollDelayMs, cancellationToken);
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"McComms.Socket ERROR: {ex.Message}");
            throw;
        }
        finally {
            ArrayPool<byte>.Shared.Return(buffer);
            RemoveClient(client);
            Debug.WriteLine($"Command client {client.Id} disconnected and scheduled for removal");
        }
    }

    /// <summary>
    /// Removes a specific disconnected client from the collection immediately.
    /// </summary>
    /// <param name="clientToRemove">The client to remove and dispose.</param>
    private void RemoveClient(SocketsClientModel clientToRemove) {
        // No lock needed - TryRemove is atomic
        if (_commandClients.TryRemove(clientToRemove.Id, out var removedClient)) {
            try {
                removedClient.Dispose();
                Debug.WriteLine($"Command client {clientToRemove.Id} removed and disposed, remaining clients: {_commandClients.Count}");
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error disposing removed command client {clientToRemove.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the SocketsServer instance.
    /// </summary>
    public void Dispose() {
        try {
            _commandListener?.Close();
            _broadcastListener?.Close();
            _commandListener?.Dispose();
            _broadcastListener?.Dispose();

            foreach (var client in _commandClients.Values) {
                if (client.Connected) {
                    client.Dispose();
                }
            }

            foreach (var client in _broadcastClients.Values) {
                if (client.Connected) {
                    client.Dispose();
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error disposing SocketsServer: {ex.Message}");
        }
        _commandClients.Clear();
        _broadcastClients.Clear();

        _messageProcessingLock?.Dispose();

        GC.SuppressFinalize(this);
    }
}

