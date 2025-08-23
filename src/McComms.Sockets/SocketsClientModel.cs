using System.Diagnostics;

namespace McComms.Sockets;

/// <summary>
/// Represents a client model for socket communications.
/// Encapsulates a client socket and its associated network stream.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SocketsClientModel"/> class.
/// </remarks>
/// <param name="clientSocket">The connected client socket.</param>
/// <param name="stream">The network stream associated with the socket.</param>
/// <param name="bufferSize">The buffer size for reading data. Default is 1500 bytes.</param>
public class SocketsClientModel(Socket clientSocket, NetworkStream stream, int bufferSize = 1500) : IDisposable {
    private readonly Socket _clientSocket = clientSocket;
    private readonly NetworkStream _stream = stream;

    /// <summary>
    /// Buffer for accumulating message content between STX and ETX characters.
    /// </summary>
    public List<byte> MessageBuffer { get; } = new List<byte>(bufferSize);

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
    public bool Connected { get; set; } = true;

    /// <summary>
    /// Gets or sets the unique identifier for the client.
    /// </summary>
    public int Id { get; set; } = 0;

    /// <summary>
    /// Disposes of the client resources including socket and stream.
    /// </summary>
    public void Dispose() {
        try {
            Connected = false;
            _stream?.Close();
            _stream?.Dispose();
            if (_clientSocket?.Connected == true) {
                _clientSocket.Shutdown(SocketShutdown.Both);
            }
            _clientSocket?.Close();
            _clientSocket?.Dispose();
        }
        catch (Exception ex) {
            Debug.WriteLine($"Error disposing SocketsClientModel: {ex.Message}");
        }
        finally {
            GC.SuppressFinalize(this);
        }
    }
}
