Console.Clear();
Console.WriteLine("McComms Demo Server");

CancellationTokenSource cts = new CancellationTokenSource();

ICommsServer? CommsServer = SelectServer();

var token = cts.Token;
CommsServer?.Start(OnCommandReceived, token);

while (token.IsCancellationRequested == false)
{
    await Task.Delay(100, token);
}

static CommandResponse OnCommandReceived(CommandRequest request)
{
    Console.WriteLine($"Command received: {request}");
    return MsgHelper.Ok("OK");
}

static ICommsServer SelectServer()
{
    Console.WriteLine("Please select a protocol:");
    Console.WriteLine("0. Quit");
    Console.WriteLine("1. gRPC");
    Console.WriteLine("2. Sockets");
    Console.WriteLine("3. WebSockets");

    while (true)
    {
        Console.Write("Enter your selection: ");

        string? input = Console.ReadLine();

        switch (input)
        {
            case "0":
                Environment.Exit(0);
                break;
            case "1":
                Console.WriteLine("gRPC server selected.");
                return new CommsServerSockets();
            case "2":
                Console.WriteLine("Sockets server selected.");
                return new CommsServerSockets();
            case "3":
                Console.WriteLine("WebSockets server selected.");
                return new CommsServerSockets();
            default:
                Console.WriteLine("Invalid selection. Please try again.");
                continue;
        }
    }
}
