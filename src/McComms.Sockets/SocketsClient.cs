using System.Runtime.InteropServices;

namespace McComms.Sockets;

/// <summary>
/// SocketsClient provides a TCP client for sending and receiving framed messages asynchronously.
/// Uses two separate connections: one for commands/responses and another for broadcast messages.
/// This eliminates race conditions between command responses and broadcast messages.
/// </summary>
public class SocketsClient : IDisposable {
    // Constants
    public const string DEFAULT_HOST = "127.0.0.1";

    public const int DEFAULT_PORT = 50051;

    // Maximum buffer size for reading messages
    private const int MAX_BUFFER_SIZE = 1500;

    // Default poll delay in milliseconds to avoid busy waiting
    private const int DEFAULT_POLL_DELAY_MS = 5;

    // Default timeout for reading messages in milliseconds
    private const int DEFAULT_READ_TIMEOUT_MS = 10000;

    // Timeout for reading messages in milliseconds
    private readonly int _readTimeoutMs;

    // Command socket and stream for sending commands and receiving responses
    private readonly Socket _commandSocket;
    private readonly IPEndPoint _commandEndPoint;
    private NetworkStream? _commandStream;

    // Broadcast socket and stream for receiving broadcast messages (port + 1)
    private readonly Socket _broadcastSocket;
    private readonly IPEndPoint _broadcastEndPoint;
    private NetworkStream? _broadcastStream;
    
    // Poll delay in milliseconds to avoid busy waiting
    private readonly int _pollDelayMs;
    
    // Semaphore for synchronizing send operations
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    // CommsHost object for host and port information
    private readonly NetworkAddress? _address;

    /// <summary>
    /// Callback invoked when a message is received from the server.
    /// </summary>
    public Action<ReadOnlySpan<byte>>? OnMessageReceived { get; set; }

    // Task and cancellation for async background message listening
    private Task? _broadcastTask;
    private readonly CancellationTokenSource _broadcastCts = new();
    private readonly List<byte> _broadcastMsgBuffer = new(MAX_BUFFER_SIZE);
    
    /// <summary>
    /// Buffer for storing the response message when sending synchronously and asynchronously.
    /// Using a single buffer for both operations since they are synchronized by semaphore.
    /// </summary>
    private readonly List<byte> _responseBuffer = new(MAX_BUFFER_SIZE);

    /// <summary>
    /// Default constructor. Connects to localhost and default port.
    /// </summary>
    public SocketsClient()
        : this(IPAddress.Parse(DEFAULT_HOST), DEFAULT_PORT, DEFAULT_POLL_DELAY_MS, DEFAULT_READ_TIMEOUT_MS) {
    }

    /// <summary>
    /// Constructor with custom host and port.
    /// </summary>
    public SocketsClient(IPAddress host, int port) 
        : this(host, port, DEFAULT_POLL_DELAY_MS, DEFAULT_READ_TIMEOUT_MS) {
    }

    /// <summary>
    /// Constructor with custom host, port, and poll delay.
    /// </summary>
    /// <param name="host">IP address of the host to connect to</param>
    /// <param name="port">Port number to use for commands</param>
    /// <param name="pollDelayMs">Delay in milliseconds between polls when no data is available</param>
    public SocketsClient(IPAddress host, int port, int pollDelayMs = DEFAULT_POLL_DELAY_MS, int readTimeoutMs = DEFAULT_READ_TIMEOUT_MS) {
        _address = new CommsHost(host.ToString(), port);
        _commandEndPoint = new IPEndPoint(host, port);
        _broadcastEndPoint = new IPEndPoint(host, port + 1);
        _commandSocket = new Socket(_commandEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _broadcastSocket = new Socket(_broadcastEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _pollDelayMs = pollDelayMs > 0 ? pollDelayMs : DEFAULT_POLL_DELAY_MS;
        _readTimeoutMs = readTimeoutMs > 0 ? readTimeoutMs : DEFAULT_READ_TIMEOUT_MS;
    }

    /// <summary>
    /// Gets the CommsHost object that contains the host and port information
    /// </summary>
    public NetworkAddress Address => _address ?? throw new InvalidOperationException("CommsHost is not initialized. Please ensure the client is properly constructed.");

    /// <summary>
    /// Gets the current state of the socket connections.
    /// </summary>
    public bool IsConnected => _commandSocket.Connected && _broadcastSocket.Connected;

    /// <summary>
    /// Connects to a server and starts background message listening (synchronous version).
    /// </summary>
    public bool Connect(Action<ReadOnlySpan<byte>> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;

        try {
            // Connect to the command server endpoint
            _commandSocket.Connect(_commandEndPoint);
            if (_commandSocket.Connected == false) {
                throw new Exception("Cannot connect to the command server.");
            }

            // Connect to the broadcast server endpoint (port + 1)
            _broadcastSocket.Connect(_broadcastEndPoint);
            if (_broadcastSocket.Connected == false) {
                throw new Exception("Cannot connect to the broadcast server.");
            }

            // Create the network streams for communication
            _commandStream = new NetworkStream(_commandSocket);
            _broadcastStream = new NetworkStream(_broadcastSocket);

            // Start the async broadcast message listener in a separate thread
            _broadcastTask = Task.Run(async () => await WaitBroadcastMessageAsync());

            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR connecting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects to the server and starts background message listening asynchronously.
    /// </summary>
    /// <param name="onMessageReceived">Callback for received messages</param>
    /// <returns>True if connected</returns>
    public async Task<bool> ConnectAsync(Action<ReadOnlySpan<byte>> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;

        try {
            // Connect to the command server endpoint
            await _commandSocket.ConnectAsync(_commandEndPoint);
            if (_commandSocket.Connected == false) {
                throw new Exception("Cannot connect to the command server.");
            }

            // Connect to the broadcast server endpoint (port + 1)
            await _broadcastSocket.ConnectAsync(_broadcastEndPoint);
            if (_broadcastSocket.Connected == false) {
                throw new Exception("Cannot connect to the broadcast server.");
            }

            // Create the network streams for communication
            _commandStream = new NetworkStream(_commandSocket);
            _broadcastStream = new NetworkStream(_broadcastSocket);

            // Start the async broadcast message listener in a separate thread
            _broadcastTask = Task.Run(async () => await WaitBroadcastMessageAsync());

            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR connecting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sends a message to the server and waits for a framed response (synchronous).
    /// </summary>
    /// <param name="message">Framed message to send</param>
    /// <returns>Response message bytes</returns>
    public byte[] Send(byte[] message) {
        if (_commandSocket?.Connected == false) {
            throw new InvalidOperationException("Command socket not connected");
        }

        if (message.Length == 0) {
            throw new InvalidOperationException("Message is empty");
        }

        // Check if the message is properly framed with STX and ETX
        if (message[0] != SocketsHelper.STX || message[^1] != SocketsHelper.ETX) {
            throw new InvalidOperationException("Message is not properly framed. Expected: [STX] + Message + [ETX]");
        }

        try {
            // Acquire send semaphore to prevent concurrent sends
            _sendSemaphore.Wait();
            bool hasResponse = false;

            // Send the message on the command stream
            _commandStream!.Write(message);

            // Get a buffer from the ArrayPool
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);
            try {
                // Wait for the response
                int posBuffer = -1;
                bool foundSTX = false;
                _responseBuffer.Clear();

                while (!hasResponse) {
                    var bytesRead = _commandStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) {
                        continue;
                    }

                    for (int x = 0; x < bytesRead; x++) {
                        if (!foundSTX) {
                            if (buffer[x] == SocketsHelper.STX) {
                                foundSTX = true;
                                posBuffer = 0;
                                _responseBuffer.Clear();
                            }
                            // Ignore all bytes before STX
                            continue;
                        }
                        if (buffer[x] == SocketsHelper.ETX) {
                            hasResponse = true;
                            break;
                        }
                        if (posBuffer >= 0) {
                            _responseBuffer.Add(buffer[x]);
                            posBuffer++;
                        }
                    }
                }

                if (posBuffer == 0) {
                    throw new Exception("No data received");
                }

                // Create the result array
                return [.. _responseBuffer];
            }
            finally {
                // Return the buffer to the pool to avoid memory leaks
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR sending message: {ex.Message}");
            throw;
        }
        finally {
            // Release send semaphore
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// Sends a message to the server and waits asynchronously for a framed response.
    /// </summary>
    /// <param name="message">Framed message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response message bytes</returns>
    public async Task<byte[]> SendAsync(byte[] message, CancellationToken cancellationToken = default) {
        if (_commandSocket?.Connected == false) {
            throw new InvalidOperationException("Command socket not connected");
        }        if (message.Length == 0) {
            throw new InvalidOperationException("Message is empty");
        }
        
        if (message[0] != SocketsHelper.STX || message[^1] != SocketsHelper.ETX) {
            throw new InvalidOperationException("Message is not properly framed. Expected: [STX] + Message + [ETX]");
        }
        
        using var timeOutCts = new CancellationTokenSource(_readTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeOutCts.Token, cancellationToken);
        var combinedToken = linkedCts.Token;
        try {
            // Acquire send semaphore
            await _sendSemaphore.WaitAsync(combinedToken);

            // Send the message on the command stream
            await _commandStream!.WriteAsync(message, combinedToken);

            // Get a buffer from the ArrayPool
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);

            try {
                // Wait for the response
                bool hasResponse = false;
                int posBuffer = -1;
                bool foundSTX = false;
                _responseBuffer.Clear();
                while (!hasResponse && !combinedToken.IsCancellationRequested) {
                    var bytesRead = await _commandStream.ReadAsync(buffer, combinedToken);
                    if (bytesRead == 0) {
                        continue;
                    }

                    for (int x = 0; x < bytesRead; x++) {
                        if (!foundSTX) {
                            if (buffer[x] == SocketsHelper.STX) {
                                foundSTX = true;
                                posBuffer = 0;
                                _responseBuffer.Clear();
                            }
                            // Ignore all bytes before STX
                            continue;
                        }
                        if (buffer[x] == SocketsHelper.ETX) {
                            hasResponse = true;
                            break;
                        }
                        if (posBuffer >= 0) {
                            _responseBuffer.Add(buffer[x]);
                            posBuffer++;
                        }
                    }
                }

                if (posBuffer == 0) {
                    throw new Exception("No data received");
                }

                return [.. _responseBuffer];
            }
            finally {
                // Return the buffer to the pool
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) {
            if (combinedToken.IsCancellationRequested) {
                System.Diagnostics.Debug.WriteLine("McComms.Socket: Operation was cancelled due to timeout");
            }
            else {
                System.Diagnostics.Debug.WriteLine("McComms.Socket: Operation was cancelled");
            }
            throw;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR sending message asynchronously: {ex.Message}");
            throw;
        }
        finally {
            // Release send semaphore
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// Asynchronously listens for broadcast messages from the server and invokes the callback.
    /// </summary>
    public async Task WaitBroadcastMessageAsync() {
        var cancellationToken = _broadcastCts.Token;

        // Rent buffer from ArrayPool for better memory efficiency
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);

        try {
            _broadcastMsgBuffer.Clear();
            
            int posBuffer = 0;
            bool receivingMessage = false;

            while (_broadcastSocket.Connected && !cancellationToken.IsCancellationRequested) {
                // Process broadcast messages on the dedicated broadcast stream
                if (_broadcastStream!.DataAvailable) {
                    // Create a linked cancellation token with timeout
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(_readTimeoutMs);
                    
                    int bytesRead;
                    try {
                        bytesRead = await _broadcastStream.ReadAsync(buffer, timeoutCts.Token);
                        if (bytesRead <= 0 || cancellationToken.IsCancellationRequested) {
                            continue;
                        }
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
                        // Just log the timeout and continue, as broadcast reading is continuous
                        System.Diagnostics.Debug.WriteLine($"Broadcast read operation timed out after {_readTimeoutMs} ms");
                        continue;
                    }

                    bool foundSTX = false;
                    for (int x = 0; x < bytesRead; x++) {
                        if (!foundSTX) {
                            if (buffer[x] == SocketsHelper.STX) {
                                foundSTX = true;
                                _broadcastMsgBuffer.Clear();
                                receivingMessage = true;
                                posBuffer = 0;
                            }
                            // Ignore all bytes before STX
                            continue;
                        }
                        if (buffer[x] == SocketsHelper.ETX && receivingMessage && posBuffer > 0) {
                            OnMessageReceived?.Invoke(CollectionsMarshal.AsSpan(_broadcastMsgBuffer));
                            receivingMessage = false;
                            _broadcastMsgBuffer.Clear();
                            posBuffer = 0;
                            foundSTX = false;
                            continue;
                        }
                        if (receivingMessage) {
                            _broadcastMsgBuffer.Add(buffer[x]);
                            posBuffer++;
                        }
                    }
                }

                // Avoid busy waiting
                await Task.Delay(_pollDelayMs, cancellationToken);
            }
        }
        catch (OperationCanceledException) {
            // Expected during cancellation, don't need to do anything
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR in broadcast listener: {ex.Message}");
            throw;
        }
        finally {
            // Return the buffer to the pool
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Disconnects from the server and stops background message listening.
    /// </summary>
    public void Disconnect() {
        try {
            // Signal the async broadcast task to cancel and exit
            _broadcastCts?.Cancel();

            // Wait for the broadcast task to finish with a reasonable timeout
            if (_broadcastTask is not null && !_broadcastTask.IsCompleted) {
                try {
                    _broadcastTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch (OperationCanceledException) { /* Expected during cancellation */
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error waiting for broadcast task: {ex.Message}");
                }
            }

            // Disconnect and close both sockets if connected
            if (_commandSocket is not null && _commandSocket.Connected) {
                _commandSocket?.Disconnect(false);
                _commandSocket?.Close();
            }

            if (_broadcastSocket is not null && _broadcastSocket.Connected) {
                _broadcastSocket?.Disconnect(false);
                _broadcastSocket?.Close();
            }
            
            // Clear event handlers to prevent memory leaks
            OnMessageReceived = null;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR disconnecting: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously disconnects from the server and stops background message listening.
    /// </summary>
    public async Task DisconnectAsync() {
        try {
            // Signal the async broadcast task to cancel and exit
            if (_broadcastCts is not null) {
                await _broadcastCts.CancelAsync();
            }

            // Wait for the broadcast task to finish with a reasonable timeout
            if (_broadcastTask is not null && !_broadcastTask.IsCompleted) {
                try {
                    _broadcastTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch (OperationCanceledException) { /* Expected during cancellation */
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error waiting for broadcast task: {ex.Message}");
                }
            }

            // Disconnect and close both sockets if connected
            if (_commandSocket is not null && _commandSocket.Connected) {
                _commandSocket.Disconnect(false);
                _commandSocket?.Close();
            }

            if (_broadcastSocket is not null && _broadcastSocket.Connected) {
                _broadcastSocket.Disconnect(false);
                _broadcastSocket?.Close();
            }
            
            // Clear event handlers to prevent memory leaks
            OnMessageReceived = null;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR disconnecting: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes the client and disconnects from the server.
    /// </summary>
    public void Dispose() {
        Disconnect();
        
        // Clear event handlers
        OnMessageReceived = null;
        
        _sendSemaphore?.Dispose();
        _broadcastCts?.Dispose();
        _broadcastMsgBuffer?.Clear();
        _responseBuffer?.Clear();
        // Don't dispose the task - just let it be collected by GC
        _commandStream?.Dispose();
        _broadcastStream?.Dispose();
        _commandSocket?.Dispose();
        _broadcastSocket?.Dispose();
        GC.SuppressFinalize(this);
    }
}
