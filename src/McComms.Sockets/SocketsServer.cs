using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace McComms.Sockets;

/// <summary>
/// Provides socket server functionality for handling client connections, 
/// message processing, and broadcasting.
/// </summary>
public class SocketsServer : IDisposable {
    /// <summary>
    /// TCP listener socket for accepting incoming client connections.
    /// </summary>
    private readonly Socket _tcpListener;

    /// <summary>
    /// Endpoint for the TCP listener defining the IP address and port to listen on.
    /// </summary>
    private readonly IPEndPoint? _tcpEndPoint = null;

    /// <summary>
    /// Thread-safe collection of currently connected clients.
    /// </summary>
    private readonly ConcurrentBag<SocketsClientModel> _clients = [];

    /// <summary>
    /// Counter for assigning unique client IDs.
    /// </summary>
    private int _nextClientId = 1;

    /// <summary>
    /// Callback function for handling received messages from clients.
    /// </summary>
    private Func<byte[], byte[]>? _onMessageReceived;

    /// <summary>
    /// Maximum buffer size for message handling.
    /// </summary>
    private const int MAX_BUFFER_SIZE = 1500;

    /// <summary>
    /// Default port number for the server when not specified.
    /// </summary>
    private const int DEFAULT_PORT = 8888;

    /// <summary>
    /// Default poll delay in milliseconds to avoid busy waiting.
    /// </summary>
    private const int DEFAULT_POLL_DELAY_MS = 5;

    /// <summary>
    /// Poll delay in milliseconds to avoid busy waiting.
    /// </summary>
    private readonly int _pollDelayMs;

    /// <summary>
    /// Initializes a new instance of the SocketsServer class with default settings.
    /// Listens on any IP address and the default port.
    /// </summary>
    public SocketsServer() : this(IPAddress.Any, DEFAULT_PORT, DEFAULT_POLL_DELAY_MS) {
    }

    /// <summary>
    /// Initializes a new instance of the SocketsServer class with the specified IP address and port.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use.</param>
    public SocketsServer(IPAddress ipAddress, int port) : this(ipAddress, port, DEFAULT_POLL_DELAY_MS) {
    }

    /// <summary>
    /// Initializes a new instance of the SocketsServer class with the specified IP address, port, and poll delay.
    /// </summary>
    /// <param name="ipAddress">The IP address to listen on.</param>
    /// <param name="port">The port number to use.</param>
    /// <param name="pollDelayMs">The delay in milliseconds between polls when no data is available.</param>
    public SocketsServer(IPAddress ipAddress, int port, int pollDelayMs) {
        _tcpEndPoint = new IPEndPoint(ipAddress, port);
        _tcpListener = new Socket(_tcpEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _pollDelayMs = pollDelayMs > 0 ? pollDelayMs : DEFAULT_POLL_DELAY_MS;
    }

    /// <summary>
    /// Starts listening for incoming client connections and processes their messages asynchronously.
    /// </summary>
    /// <param name="onMessageReceived">Callback function invoked when a message is received from a client.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ListenAsync(Func<byte[], byte[]> onMessageReceived, CancellationToken stopToken) {
        _onMessageReceived = onMessageReceived;

        _tcpListener.Bind(_tcpEndPoint!);
        _tcpListener.Listen();

        // Main accept loop
        while (stopToken.IsCancellationRequested == false) {
            try {
                Socket client = await Task.Run(() => _tcpListener.Accept(), stopToken);
                var socketClient = new SocketsClientModel(client, new NetworkStream(client), MAX_BUFFER_SIZE);

                // Assign ID atomically and add to thread-safe collection
                socketClient.Id = Interlocked.Increment(ref _nextClientId);
                _clients.Add(socketClient);

                Debug.WriteLine($"Client connected, total clients: {_clients.Count}");

                // Handle client messages in a background task
                _ = Task.Run(async () => await MessagesHandler(socketClient, stopToken), stopToken);
            }
            catch (OperationCanceledException) {
                // Cancellation requested, break out of the loop
                break;
            }
            catch (Exception ex) {
                Debug.WriteLine($"McComms.Socket ERROR accepting client: {ex.Message}");
                // Continue listening for next connection
            }
        }
    }

    /// <summary>
    /// Starts listening for incoming client connections and processes their messages.
    /// </summary>
    /// <param name="onMessageReceived">Callback function invoked when a message is received from a client.</param>
    /// <param name="stopToken">A cancellation token that can be used to stop the server.</param>
    public void Listen(Func<byte[], byte[]> onMessageReceived, CancellationToken stopToken) {
        // Call the async version and block until it completes
        // This maintains backward compatibility
        _ = Task.Run(async () => await ListenAsync(onMessageReceived, stopToken), stopToken);
    }

    /// <summary>
    /// Sends a broadcast message to all connected clients asynchronously.
    /// </summary>
    /// <param name="command">The message to broadcast as a byte array.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async ValueTask SendBroadcastAsync(byte[] command) {
        int disconnectedCount = 0;

        // Process only connected clients
        foreach (var client in _clients) {
            try {
                if (client.Connected) {
                        await client.Stream.WriteAsync(command);
                }
            }
            catch {
                // Just mark client as disconnected, no need to remove from the collection
                client.Connected = false;
                disconnectedCount++;
            }
        }

        // Log disconnected clients if any
        if (disconnectedCount > 0) {
            Debug.WriteLine($"{disconnectedCount} client(s) marked as disconnected, total clients in collection: {_clients.Count}");
        }
    }

    /// <summary>
    /// Handles messages from a specific client, processing incoming data according to the protocol.
    /// </summary>
    /// <param name="client">The client model representing the connected client.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to stop message handling.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>    
    private async Task MessagesHandler(SocketsClientModel client, CancellationToken cancellationToken) {        Debug.Assert(client != null);
        // Get direct reference to message buffer
        var bufferMessage = client.MessageBuffer;
        
        // Use ArrayPool to rent a buffer instead of allocating a new one
        // More efficient than creating a new array for each client
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);

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
                                break;
                            case SocketsHelper.STX:
                                // Start of new message
                                bufferMessage.Clear();
                                receivingMessage = true;
                                break;
                            case SocketsHelper.ETX:
                                // End of message: process and respond
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
                                // Buffer message content if inside a message
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
            // Just ensure client is marked as disconnected
            // No need to remove from the collection
            client.Connected = false;
            Debug.WriteLine($"Client disconnected, total clients in collection: {_clients.Count}");
        }
    }

    /// <summary>
    /// Releases all resources used by the SocketsServer instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the SocketsServer and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Close the TCP listener socket
            if (_tcpListener != null)
            {
                try
                {
                    _tcpListener.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during socket close: {ex.Message}");
                }
            }

            // Close all client connections
            foreach (var client in _clients)
            {
                try
                {                    if (client.Connected)
                    {
                        client.Connected = false;
                        client.Stream?.Close();
                        client.ClientSocket?.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error closing client connection: {ex.Message}");
                }
            }

            _clients.Clear();
        }
    }
}

