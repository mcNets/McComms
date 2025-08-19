namespace McComms.Sockets;

/// <summary>
/// Represents a client model for socket communications.
/// Encapsulates a client socket and its associated network stream.
/// </summary>
public class SocketsClientModel : IDisposable {
    private readonly Socket _clientSocket;

    private readonly NetworkStream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketsClientModel"/> class.
    /// </summary>
    /// <param name="clientSocket">The connected client socket.</param>
    /// <param name="stream">The network stream associated with the socket.</param>
    /// <param name="bufferSize">The buffer size for reading data. Default is 1500 bytes.</param>
    public SocketsClientModel(Socket clientSocket, NetworkStream stream, int bufferSize = 1500) {
        _clientSocket = clientSocket;
        _stream = stream;
        Connected = true;
        Id = 0;
        MessageBuffer = new List<byte>(bufferSize);
    }

    /// <summary>
    /// Buffer for accumulating message content between STX and ETX characters.
    /// </summary>
    public List<byte> MessageBuffer { get; }

    /// <summary>
    /// Gets the underlying client socket.
    /// </summary>
    public Socket ClientSocket => _clientSocket;

    /// <summary>
    /// Gets the network stream associated with the client.
    /// </summary>
    public NetworkStream Stream => _stream;

    /// <summary>
    /// Gets or sets a value indicating whether the client is connected.
    /// </summary>
    public bool Connected { get; set; } = false;

    /// <summary>
    /// Gets or sets the unique identifier for the client.
    /// </summary>
    public int Id { get; set; } = 0;

    /// <summary>
    /// Disposes of the client resources including socket and stream.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method for proper resource cleanup.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            try {
                // Mark as disconnected first
                Connected = false;
                
                // Close and dispose the stream
                _stream?.Close();
                _stream?.Dispose();
                
                // Close and dispose the socket
                if (_clientSocket?.Connected == true) {
                    _clientSocket.Shutdown(SocketShutdown.Both);
                }
                _clientSocket?.Close();
                _clientSocket?.Dispose();
                
                // Clear the message buffer
                MessageBuffer?.Clear();
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error disposing SocketsClientModel: {ex.Message}");
            }
        }
    }
}
