using DemoServer;

Console.Clear();
Console.WriteLine("McComms Demo Server");

CancellationTokenSource cts = new CancellationTokenSource();

ICommsServer? commsServer = null;
 
Protocol server = new Protocol();

switch (server.Protocol)
{
    case ProtocolType.gRPC:
        //commsServer = new GrpcServer();
        break;
    case ProtocolType.Sockets:
        commsServer = new CommsServerSockets();
        break;
    case ProtocolType.WebSockets:
        //commsServer = new WebSocketServer();
        break;
}

if (commsServer == null)
{
    Console.WriteLine("No server selected. Exiting...");
    return;
}

Console.WriteLine($"Starting {server.Protocol} server...");

var token = cts.Token;
commsServer.Start(OnCommandReceived, token);

while (token.IsCancellationRequested == false)
{
    await Task.Delay(100, token);
}

CommandResponse OnCommandReceived(CommandRequest request)
{
    Console.WriteLine($"Command received: {request}");
    return MsgHelper.Ok("OK");
}