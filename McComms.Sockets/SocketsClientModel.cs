namespace McComms.Sockets;

public class SocketsClientModel
{
    private readonly Socket _clientSocket;
    private readonly NetworkStream _stream;

    public SocketsClientModel(Socket clientSocket, NetworkStream stream) {
        _clientSocket = clientSocket;
        _stream = stream;
        Connected = true;
        Id = 0;
    }

    public Socket ClientSocket => _clientSocket;

    public NetworkStream Stream => _stream;

    public bool Connected { get; set; } = true;

    public int Id { get; set; } = 0;
}
