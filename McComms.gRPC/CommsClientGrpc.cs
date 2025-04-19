using Commsproto;
using mcNuget.Comms.gRPC;

namespace mcNuget.Comms.gRPC;

public class CommsClientGrpc : ICommsClient
{
    private readonly GrpcClient _client;

    public CommsClientGrpc() {
        _client = new GrpcClient();       
    }

    public CommsClientGrpc(string host, int port) {
        _client = new GrpcClient(host, port);
    }

    public Action<BroadcastMessage>? OnBroadcastReceived { get; set; }

    public bool Connect(Action<BroadcastMessage>? onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;
        return _client.Connect(BroadcastReceived);
    }

    private void BroadcastReceived(mcBroadcast message) {
        OnBroadcastReceived?.Invoke(new BroadcastMessage(message.Id, message.Content));
    }

    public void Disconnect() {
        _client.Disconnect();
    }

    public CommandResponse SendCommand(CommandRequest msg) {
        var response = _client.SendCommand(new mcCommandRequest { Id = msg.Id, Content = msg.Message });
        return new CommandResponse(response.Succes, response.Id, response.Message);
    }

    public void SendExitCommand() {
        Disconnect();
    }
}