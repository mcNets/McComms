using System.Threading.Tasks;

namespace McComms.Sockets;

/// <summary>
/// SocketsClient provides a TCP client for sending and receiving framed messages asynchronously.
/// Handles connection, message sending, and background message listening.
/// </summary>
public class SocketsClient : IDisposable
{
    // Underlying TCP socket
    private readonly Socket _socket;
    // Server endpoint to connect to
    private readonly IPEndPoint _endPoint;
    // Network stream for reading/writing data
    private NetworkStream? _stream;
    // Buffer size constants
    private const int MAX_BUFFER_SIZE = 1500;
    private const int MAX_BUFFER_SIZE_MESSAGE = MAX_BUFFER_SIZE + 200;
    private const int DEFAULT_PORT = 8888;
    // Buffers for sending/receiving data
    private readonly byte[] _sendBuffer = new byte[MAX_BUFFER_SIZE];
    private readonly byte[] _sendBufferMessage = new byte[MAX_BUFFER_SIZE_MESSAGE];
    // ManualResetEvent for synchronizing broadcast message handling
    private static readonly ManualResetEvent _broadcastSignal = new(true);

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
    public SocketsClient() {
        _endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), DEFAULT_PORT);
        _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }
    /// <summary>
    /// Constructor with custom host and port.
    /// </summary>
    public SocketsClient(IPAddress host, int port) {
        _endPoint = new IPEndPoint(host, port);
        _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    /// <summary>
    /// Connects to the server and starts background message listening asynchronously.
    /// </summary>
    /// <param name="onMessageReceived">Callback for received messages</param>
    /// <returns>True if connected</returns>
    public async Task<bool> ConnectAsync(Action<byte[]> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;
        // Connect to the server endpoint
        _socket.Connect(_endPoint);
        if (_socket.Connected == false) {
            throw new Exception("Cannot connect to the server.");
        }
        // Create the network stream for communication
        _stream = new NetworkStream(_socket);
        // Start the async broadcast message listener
        _broadcastCts = new CancellationTokenSource();
        _broadcastTask = WaitBroadcastMessageAsync(_broadcastCts.Token);
        await Task.Yield(); // Ensure method is truly async
        return true;
    }

    /// <summary>
    /// Connects to the server and starts background message listening (synchronous version).
    /// </summary>
    public bool Connect(Action<byte[]> onMessageReceived) {
        // Set the callback for received messages
        OnMessageReceived = onMessageReceived;
        // Connect to the server endpoint
        _socket.Connect(_endPoint);
        if (_socket.Connected == false) {
            throw new Exception("Cannot connect to the server.");
        }
        // Create the network stream for communication
        _stream = new NetworkStream(_socket);
        // Start the async broadcast message listener
        _broadcastCts = new CancellationTokenSource();
        _broadcastTask = WaitBroadcastMessageAsync(_broadcastCts.Token);
        return true;
    }

    /// <summary>
    /// Disconnects from the server and stops background message listening.
    /// </summary>
    public void Disconnect() {
        // Signal the async broadcast task to cancel and exit
        _broadcastSignal.Set();
        _broadcastCts?.Cancel();
        // Disconnect and close the socket if connected
        if (_socket is not null && _socket.Connected) {
            _socket?.Disconnect(false);
            _socket?.Close();
        }
        _socket?.Dispose();
        // Wait for the broadcast task to finish
        if (_broadcastTask is not null) {
            try { _broadcastTask.Wait(); } catch { /* ignore */ }
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
        if (message[0] != SocketsHelper.STX || message[^1] != SocketsHelper.ETX) {
            throw new Exception("Message is not properly framed. Expected: [STX] + Message + [ETX]");
        }

        int posBuffer = -1;

        _broadcastSignal.Reset();

        // send the message
        _stream!.Write(message);

        // wait for the response
        bool hasResponse = false;
        while (hasResponse == false) {
            try {
                var bytesRead = _stream.Read(_sendBuffer);
                if (bytesRead == 0) {
                    throw new Exception("No data received");
                }

                for (int x = 0; x < bytesRead; x++) {
                    if (_sendBuffer[x] == SocketsHelper.STX) {
                        posBuffer = 0;
                        continue;
                    }
                    else if (_sendBuffer[x] == SocketsHelper.ETX && posBuffer > 0) {
                        hasResponse = true;
                        break;
                    }
                    else if (posBuffer >= 0 && posBuffer < MAX_BUFFER_SIZE_MESSAGE) {
                        _sendBufferMessage[posBuffer++] = _sendBuffer[x];
                    }
                    else {
                        throw new Exception($"PosBuffer out of range (0-{MAX_BUFFER_SIZE_MESSAGE})");
                    }
                }
            }
            catch {
                throw;
            }
        }

        byte[] msg = new byte[posBuffer];
        _sendBufferMessage[..posBuffer].CopyTo(msg, 0);
        _broadcastSignal?.Set();
        return msg;
    }

    /// <summary>
    /// Asynchronously listens for broadcast messages from the server and invokes the callback.
    /// </summary>
    public async Task WaitBroadcastMessageAsync(CancellationToken cancellationToken = default) {
        byte[] _broadcastBuffer = new byte[MAX_BUFFER_SIZE];
        List<byte> _broadcastBufferMessage = new(MAX_BUFFER_SIZE);
        int posBuffer = 0;
        while (_socket.Connected && !cancellationToken.IsCancellationRequested) {
            _broadcastSignal.WaitOne();
            if (_stream!.DataAvailable) {
                try {
                    var bytesRead = await _stream.ReadAsync(_broadcastBuffer, cancellationToken);
                    if (bytesRead == 0) {
                        throw new Exception("No data received");
                    }
                    for (int x = 0; x < bytesRead; x++) {
                        if (_broadcastBuffer[x] == SocketsHelper.STX) {
                            posBuffer = 0;
                            _broadcastBufferMessage.Clear();
                            continue;
                        }
                        else if (_broadcastBuffer[x] == SocketsHelper.ETX && posBuffer > 0) {
                            var msg = _broadcastBufferMessage.ToArray();
                            OnMessageReceived?.Invoke(msg);
                            posBuffer = -1;
                            _broadcastBufferMessage.Clear();
                            break;
                        }
                        else if (posBuffer >= 0) {
                            _broadcastBufferMessage.Add(_broadcastBuffer[x]);
                            posBuffer++;
                        }
                        else {
                            throw new Exception($"PosBuffer out of range (0-{MAX_BUFFER_SIZE})");
                        }
                    }
                }
                catch {
                    throw;
                }
            } else {
                await Task.Delay(5, cancellationToken);
            }
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

        int posBuffer = -1;
        _broadcastSignal.Reset();
        // send the message
        await _stream!.WriteAsync(message, cancellationToken);
        // wait for the response
        bool hasResponse = false;
        List<byte> responseBuffer = new(MAX_BUFFER_SIZE);
        while (!hasResponse && !cancellationToken.IsCancellationRequested) {
            try {
                var bytesRead = await _stream.ReadAsync(_sendBuffer, cancellationToken);
                if (bytesRead == 0) {
                    throw new Exception("No data received");
                }
                for (int x = 0; x < bytesRead; x++) {
                    if (_sendBuffer[x] == SocketsHelper.STX) {
                        posBuffer = 0;
                        responseBuffer.Clear();
                        continue;
                    }
                    else if (_sendBuffer[x] == SocketsHelper.ETX && posBuffer > 0) {
                        hasResponse = true;
                        break;
                    }
                    else if (posBuffer >= 0) {
                        responseBuffer.Add(_sendBuffer[x]);
                        posBuffer++;
                    }
                    else {
                        throw new Exception($"PosBuffer out of range (0-{MAX_BUFFER_SIZE})");
                    }
                }
            }
            catch {
                throw;
            }
        }
        _broadcastSignal?.Set();
        return responseBuffer.ToArray();
    }

    /// <summary>
    /// Disposes the client and disconnects from the server.
    /// </summary>
    public void Dispose() {
        Disconnect();
    }
}
