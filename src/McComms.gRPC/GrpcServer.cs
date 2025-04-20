namespace mcNuget.Comms.gRPC;

using System.Diagnostics;

public class GrpcServer : mcServeis.mcServeisBase
{
    private readonly List<IServerStreamWriter<mcBroadcast>> _broadcastWriters;
    private readonly Server? _server;

    private Func<mcCommandRequest, mcCommandResponse>? OnCommandReceived { get; set; }

    public GrpcServer() {
        _broadcastWriters = new();
        _server = new Server {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort("0.0.0.0", 50051, ServerCredentials.Insecure) }
        };
    }

    public GrpcServer(string host, int port) {
        _broadcastWriters = new();
        _server = new Server {
            Services = { mcServeis.BindService(this) },
            Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
        };
    }

    public void Start(Func<mcCommandRequest, mcCommandResponse>? onCommandReceived) {
        ArgumentNullException.ThrowIfNull(_server);
        this.OnCommandReceived = onCommandReceived;
        _server.Start();
    }

    public override Task<mcCommandResponse> SendCommand(mcCommandRequest request, ServerCallContext context) {
        ArgumentNullException.ThrowIfNull(OnCommandReceived);
        var response = OnCommandReceived.Invoke(new mcCommandRequest { Id = request.Id, Content = request.Content });
        return Task.FromResult(new mcCommandResponse { Succes = response.Succes, Id = response.Id, Message = response.Message });
    }

    public void SendBroadcast(mcBroadcast message) {
        Task.Run(async () => {
            foreach (var writer in _broadcastWriters) {
                await writer.WriteAsync(message);
            }
        });
    }

    public override async Task Broadcast(IAsyncStreamReader<Empty> requestStream, IServerStreamWriter<mcBroadcast> responseStream, ServerCallContext context) {
        lock (_broadcastWriters) {
            _broadcastWriters.Add(responseStream);
            Debug.WriteLine("Client connected.");
        }
        while (!context.CancellationToken.IsCancellationRequested) {
            await Task.Delay(10);
        }
        lock (_broadcastWriters) {
            _broadcastWriters.Remove(responseStream);
            Debug.WriteLine("Client disconnected.");
        }
    }
}
