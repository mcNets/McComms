namespace McComms.Sockets;

/// <summary>
/// SocketsClient provides a TCP client for sending and receiving framed messages asynchronously.
/// Handles connection, message sending, and background message listening.
/// </summary>
public class SocketsClient : IDisposable {
    // Underlying TCP socket
    private readonly Socket _socket;
    // Server endpoint to connect to
    private readonly IPEndPoint _endPoint;
    // Network stream for reading/writing data
    private NetworkStream? _stream;
    // Buffer size constants
    private const int MAX_BUFFER_SIZE = 1500;
    private const int DEFAULT_PORT = 8888;
    // Default poll delay in milliseconds to avoid busy waiting
    private const int DEFAULT_POLL_DELAY_MS = 5;
    // Poll delay in milliseconds to avoid busy waiting
    private readonly int _pollDelayMs;
    // ManualResetEvent for synchronizing broadcast message handling
    private static readonly SemaphoreSlim _broadcastSemaphore = new(1, 1);
    // Flag to indicate if a message is being sent
    private volatile bool _isSending = false;

    /// <summary>
    /// Callback invoked when a message is received from the server.
    /// </summary>
    public Action<byte[]>? OnMessageReceived { get; set; }

    // Task and cancellation for async background message listening
    private Task? _broadcastTask;
    private CancellationTokenSource? _broadcastCts;

    /// <summary>
    /// Default constructor. Connects to localhost and default port.
    /// </summary>
    public SocketsClient() : this(IPAddress.Parse("127.0.0.1"), DEFAULT_PORT, DEFAULT_POLL_DELAY_MS) {
    }

    /// <summary>
    /// Constructor with custom host and port.
    /// </summary>
    public SocketsClient(IPAddress host, int port) : this(host, port, DEFAULT_POLL_DELAY_MS) {
    }

    /// <summary>
    /// Constructor with custom host, port, and poll delay.
    /// </summary>
    /// <param name="host">IP address of the host to connect to</param>
    /// <param name="port">Port number to use</param>
    /// <param name="pollDelayMs">Delay in milliseconds between polls when no data is available</param>
    public SocketsClient(IPAddress host, int port, int pollDelayMs) {
        _endPoint = new IPEndPoint(host, port);
        _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _pollDelayMs = pollDelayMs > 0 ? pollDelayMs : DEFAULT_POLL_DELAY_MS;
    }

    /// <summary>
    /// Connects to the server and starts background message listening asynchronously.
    /// </summary>
    /// <param name="onMessageReceived">Callback for received messages</param>
    /// <returns>True if connected</returns>
    public async Task<bool> ConnectAsync(Action<byte[]> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;

        try {
            // Connect to the server endpoint
            await _socket.ConnectAsync(_endPoint);
            if (_socket.Connected == false) {
                throw new Exception("Cannot connect to the server.");
            }

            // Create the network stream for communication
            _stream = new NetworkStream(_socket);

            // Start the async broadcast message listener in a separate thread
            _broadcastCts = new CancellationTokenSource();
            _broadcastTask = Task.Run(async () => await WaitBroadcastMessageAsync(_broadcastCts.Token));

            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR connecting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Connects to the server and starts background message listening (synchronous version).
    /// </summary>
    public bool Connect(Action<byte[]> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;

        try {
            // Connect to the server endpoint
            _socket.Connect(_endPoint);
            if (_socket.Connected == false) {
                throw new Exception("Cannot connect to the server.");
            }

            // Create the network stream for communication
            _stream = new NetworkStream(_socket);

            // Start the async broadcast message listener in a separate thread
            _broadcastCts = new CancellationTokenSource();
            _broadcastTask = Task.Run(async () => await WaitBroadcastMessageAsync(_broadcastCts.Token));

            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR connecting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the server and stops background message listening.
    /// </summary>
    public void Disconnect() {
        try {
            // Signal the async broadcast task to cancel and exit
            _broadcastCts?.Cancel();

            // Disconnect and close the socket if connected
            if (_socket is not null && _socket.Connected) {
                _socket?.Disconnect(false);
                _socket?.Close();
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
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR disconnecting: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a message to the server and waits for a framed response (synchronous).
    /// </summary>
    /// <param name="message">Framed message to send</param>
    /// <returns>Response message bytes</returns>
    public byte[] Send(byte[] message) {
        if (_socket?.Connected == false) {
            throw new Exception("Socket not connected");
        }

        if (message.Length == 0) {
            throw new Exception("Message is empty");
        }

        // Check if the message is properly framed with STX and ETX
        if (message[0] != SocketsHelper.STX || message[^1] != SocketsHelper.ETX) {
            throw new Exception("Message is not properly framed. Expected: [STX] + Message + [ETX]");
        }

        try {
            // Set sending flag and acquire semaphore to prevent conflicts with broadcast receiving
            _broadcastSemaphore.Wait();
            _isSending = true;

            // Send the message
            _stream!.Write(message);

            // Get a buffer from the ArrayPool
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);
            List<byte> responseBuffer = new(MAX_BUFFER_SIZE);

            try {
                // Wait for the response
                bool hasResponse = false;
                int posBuffer = -1;

                while (hasResponse == false) {
                    var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) {
                        throw new Exception("No data received");
                    }

                    for (int x = 0; x < bytesRead; x++) {
                        if (buffer[x] == SocketsHelper.STX) {
                            posBuffer = 0;
                            responseBuffer.Clear();
                            continue;
                        }
                        else if (buffer[x] == SocketsHelper.ETX && posBuffer > 0) {
                            hasResponse = true;
                            break;
                        }
                        else if (posBuffer >= 0) {
                            responseBuffer.Add(buffer[x]);
                            posBuffer++;
                        }
                    }
                }

                // Create the result array
                return [.. responseBuffer];
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
            // Reset sending flag and release semaphore
            _broadcastSemaphore.Release();
            _isSending = false;
        }
    }

    /// <summary>
    /// Sends a message to the server and waits asynchronously for a framed response.
    /// </summary>
    /// <param name="message">Framed message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response message bytes</returns>
    public async Task<byte[]> SendAsync(byte[] message, CancellationToken cancellationToken = default) {
        if (_socket?.Connected == false) {
            throw new Exception("Socket not connected");
        }
        if (message.Length == 0) {
            throw new Exception("Message is empty");
        }
        if (message[0] != SocketsHelper.STX || message[^1] != SocketsHelper.ETX) {
            throw new Exception("Message is not properly framed. Expected: [STX] + Message + [ETX]");
        }

        try {
            // Set sending flag and acquire semaphore
            await _broadcastSemaphore.WaitAsync(cancellationToken);
            _isSending = true;

            // Send the message
            await _stream!.WriteAsync(message, cancellationToken);

            // Get a buffer from the ArrayPool
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);
            List<byte> responseBuffer = new(MAX_BUFFER_SIZE);

            try {
                // Wait for the response
                bool hasResponse = false;
                int posBuffer = -1;

                while (!hasResponse && !cancellationToken.IsCancellationRequested) {
                    var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0) {
                        throw new Exception("No data received");
                    }

                    for (int x = 0; x < bytesRead; x++) {
                        if (buffer[x] == SocketsHelper.STX) {
                            posBuffer = 0;
                            responseBuffer.Clear();
                            continue;
                        }
                        else if (buffer[x] == SocketsHelper.ETX && posBuffer > 0) {
                            hasResponse = true;
                            break;
                        }
                        else if (posBuffer >= 0) {
                            responseBuffer.Add(buffer[x]);
                            posBuffer++;
                        }
                    }
                }

                // Create the result array
                return responseBuffer.ToArray();
            }
            finally {
                // Return the buffer to the pool
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) {
            System.Diagnostics.Debug.WriteLine("McComms.Socket: Operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"McComms.Socket ERROR sending message asynchronously: {ex.Message}");
            throw;
        }
        finally {
            // Reset sending flag and release semaphore
            _isSending = false;
            _broadcastSemaphore.Release();
        }
    }

    /// <summary>
    /// Asynchronously listens for broadcast messages from the server and invokes the callback.
    /// </summary>
    public async Task WaitBroadcastMessageAsync(CancellationToken cancellationToken = default) {
        // Rent buffer from ArrayPool for better memory efficiency
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_BUFFER_SIZE);
        List<byte> messageBuffer = new(MAX_BUFFER_SIZE);

        try {
            int posBuffer = -1;
            bool receivingMessage = false;

            while (_socket.Connected && !cancellationToken.IsCancellationRequested) {
                // Skip processing if we're currently sending a message
                if (_isSending) {
                    await Task.Delay(_pollDelayMs, cancellationToken);
                    continue;
                }

                // Try to acquire the semaphore to prevent conflicts with send operations
                if (await _broadcastSemaphore.WaitAsync(0,cancellationToken)) {
                    try {
                        if (_stream!.DataAvailable) {
                            var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead <= 0) {
                                continue;
                            }

                            for (int x = 0; x < bytesRead; x++) {
                                switch (buffer[x]) {
                                    case SocketsHelper.STX:
                                        messageBuffer.Clear();
                                        receivingMessage = true;
                                        posBuffer = 0;
                                        break;
                                    case SocketsHelper.ETX:
                                        if (receivingMessage && posBuffer > 0) {
                                            var msg = messageBuffer.ToArray();
                                            OnMessageReceived?.Invoke(msg);
                                            receivingMessage = false;
                                            messageBuffer.Clear();
                                            posBuffer = -1;
                                        }
                                        break;
                                    default:
                                        if (receivingMessage) {
                                            messageBuffer.Add(buffer[x]);
                                            posBuffer++;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    finally {
                        _broadcastSemaphore.Release();
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
    /// Disposes the client and disconnects from the server.
    /// </summary>
    public void Dispose() {
        Disconnect();
        _broadcastSemaphore?.Dispose();
        _broadcastCts?.Dispose();
        _stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
