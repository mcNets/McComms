using Commsproto;
using mcNuget.Comms.gRPC;

namespace mcNuget.Comms.gRPC;

public class CommsServerGrpc : ICommsServer
{
    private readonly GrpcServer _grpcServer;

    public CommsServerGrpc() {
        _grpcServer = new GrpcServer();
    }

    public CommsServerGrpc(string host, int port) {
        _grpcServer = new GrpcServer(host, port);
    }

    public Func<CommandRequest, CommandResponse>? CommandReceived { get; set; }

    public void Start(Func<CommandRequest, CommandResponse>? onCommandReceived, CancellationToken stoppingToken) {
        CommandReceived = onCommandReceived;
        _grpcServer.Start(OnCommandReceived);
    }

    public void Stop() {
    }

    public void SendBroadcast(BroadcastMessage msg) {
        _grpcServer.SendBroadcast(new mcBroadcast { Id = msg.Id, Content = msg.Message });
    }

    private mcCommandResponse OnCommandReceived(mcCommandRequest request) {
        ArgumentNullException.ThrowIfNull(CommandReceived);
        var result = CommandReceived.Invoke(new CommandRequest(request.Id, request.Content));
        return new mcCommandResponse { Succes = result.Success, Id = result.Id, Message = result.Message };
    }
}