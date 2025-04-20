namespace McComms.Sockets;

/// <summary>
/// Represents a client model for socket communications.
/// Encapsulates a client socket and its associated network stream.
/// </summary>
public class SocketsClientModel {
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
}
