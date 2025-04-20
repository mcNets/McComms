using System.Diagnostics;

namespace mcNuget.Comms.gRPC;

public class GrpcClient
{
    private readonly Channel? _channel;
    private readonly mcServeis.mcServeisClient _client;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _broadcastTask;

    public GrpcClient() {
        _channel = new Channel("localhost", 50051, ChannelCredentials.Insecure);
        _client = new mcServeis.mcServeisClient(_channel);
    }

    public GrpcClient(string host, int port) {
        _channel = new Channel(host, port, ChannelCredentials.Insecure);
        _client = new mcServeis.mcServeisClient(_channel);
    }

    public Action<mcBroadcast>? OnBroadcastReceived { get; set; }

    public bool Connect(Action<mcBroadcast> onBroadcastReceived) {
        OnBroadcastReceived = onBroadcastReceived;

        var token = _cancellationTokenSource.Token;

        _broadcastTask = Task.Run(async () => {
            var broadcastCall = _client.Broadcast(cancellationToken: token);
            
            while (await broadcastCall.ResponseStream.MoveNext(token)) {
                try {
                    var broadcastMessage = broadcastCall.ResponseStream.Current;
                    OnBroadcastReceived?.Invoke(broadcastMessage);
                }
                catch (Exception ex){
                    Debug.WriteLine(ex.Message);
                    // todo implement error handling
                    throw;
                }
            }
        }, token);
        return true;
    }

    public async void Disconnect() {
        try {
            _cancellationTokenSource?.Cancel();

            if (_channel != null) {
                await _channel.ShutdownAsync();
            }
            
            if (_broadcastTask != null) {
                await _broadcastTask;
            }
        }
        catch {
            // errors at this point don't matter.
        }
    }

    public mcCommandResponse SendCommand(mcCommandRequest msg) {
        CallOptions options = new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddSeconds(3));

        return _client.SendCommand(msg, options:options);
    }
}
