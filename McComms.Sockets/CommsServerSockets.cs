
namespace McComms.Sockets;

public class CommsServerSockets : ICommsServer {
    private readonly SocketsServer _socketServer;

    public CommsServerSockets() {
        _socketServer = new SocketsServer();
    }

    public CommsServerSockets(IPAddress ipAddress, int port) {
        _socketServer = new SocketsServer(ipAddress, port);
    }

    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    public void Start(Func<CommandRequest, CommandResponse>? commandReceived, CancellationToken stoppingToken) {
        CommandReceived = commandReceived;
        _ = Task.Run(() => _socketServer.Listen(OnCommandReceived, stoppingToken), stoppingToken);
    }

    public void Stop() { }

    public void SendBroadcast(BroadcastMessage msg) {
        _socketServer.SendBroadcast(SocketsHelper.Encode(msg.ToString()));
    }

    private byte[] OnCommandReceived(byte[] response) {
        var cmd = SocketsHelper.Decode(response);
        if (cmd.TryParseCommandRequest(out var request) == false || request == null) {
            return SocketsHelper.Encode(MsgHelper.Fail().ToString());
        }
        var result = CommandReceived?.Invoke(request);
        var encoded = result?.ToString() ?? MsgHelper.Fail().ToString();
        return SocketsHelper.Encode(encoded);
    }
}