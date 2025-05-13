using McComms.Core;
using McComms.gRPC;
using McComms.Sockets;
using McComms.WebSockets;

Console.Clear();
Console.WriteLine("McComms Demo Client");


CancellationTokenSource cts = new CancellationTokenSource();

ICommsClient? CommsClient = SelectClient();

var token = cts.Token;
CommsClient?.Connect(OnBroadcastReceived);

while (true)
{
    Console.WriteLine("Enter a command to send to the server (or 'exit' to quit):");

    string? input = Console.ReadLine();

    if (string.IsNullOrEmpty(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    CommandRequest? commandRequest = null;
    input?.TryParseCommandRequest(out commandRequest);
    if (commandRequest == null)
    {
        Console.WriteLine("Invalid command format. Please use 'Id:Message'.");
        continue;
    }

    var resp = CommsClient?.SendCommand(commandRequest);
    if (resp == null)
    {
        Console.WriteLine("Failed to send command.");
        continue;
    }

    Console.WriteLine($"Response: {resp}");
}

static void OnBroadcastReceived(BroadcastMessage message)
{
    Console.WriteLine($"Broadcast received: {message}");
}

static ICommsClient SelectClient()
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
                Console.WriteLine("gRPC selected.");
                return new CommsClientGrpc();
            case "2":
                Console.WriteLine("Sockets selected.");
                return new CommsClientSockets();
            case "3":
                Console.WriteLine("WebSockets selected.");
                return new CommsClientWebSockets();
            default:
                Console.WriteLine("Invalid selection. Please try again.");
                continue;
        }
    }
}
