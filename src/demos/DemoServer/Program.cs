using McComms.gRPC;
using McComms.WebSockets;

Console.Clear();
Console.WriteLine("McComms Demo Server");

CancellationTokenSource cts = new CancellationTokenSource();

ICommsServer? CommsServer = SelectServer();

System.Timers.Timer timer = new System.Timers.Timer(1000);
timer.Elapsed += (sender, e) =>
{
    string dateTime = "0:" + DateTime.Now.ToString();
    Console.WriteLine($"Broadcasting clock: {dateTime}");

    dateTime.TryParseBroadcastMessage(out var msg);
    CommsServer?.SendBroadcast(new BroadcastMessage(msg.Id, msg.Message));
};

var token = cts.Token;
CommsServer?.Start(OnCommandReceived, token);

while (token.IsCancellationRequested == false)
{
    await Task.Delay(100, token);
}

CommandResponse OnCommandReceived(CommandRequest request)
{
    Console.WriteLine($"Command received: {request}\n");

    switch (request.Id)
    {
        case 0:
            return MsgHelper.Ok("Command 0, OK");
        case 1:
            string msg = """
                0 -> Returns Ok.
                1 -> Returns Commands list.
                2 -> Starts broadcast clock.
                3 -> Stops broadcast clock.
                """;
            return MsgHelper.Ok(msg);
        case 2:
            timer.Start();
            return MsgHelper.Ok("Broadcast clock started.");
        case 3:
            timer.Stop();
            return MsgHelper.Ok("Broadcast clock stopped.");
    }

    return MsgHelper.Fail("ERR01", "Command not found");
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
                Console.WriteLine("gRPC server selected.\nWaiting for commands...\n");
                return new CommsServerGrpc();
            case "2":
                Console.WriteLine("Sockets server selected.\nWaiting for commands...\n");
                return new CommsServerSockets();
            case "3":
                Console.WriteLine("WebSockets server selected.\nWaiting for commands...\n");
                return new CommsServerWebSockets();
            default:
                Console.WriteLine("Invalid selection. Please try again.");
                continue;
        }
    }
}
