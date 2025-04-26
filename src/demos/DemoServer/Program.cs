Console.WriteLine("McComms Demo Server");

ServerType protocol = ServerType.None;

while (getProtocol() == ServerType.None)
{

}

ServerType getProtocol()
{
    Console.WriteLine("Please select a protocol:");
    Console.WriteLine("0. Quit");
    Console.WriteLine("1. gRPC");
    Console.WriteLine("2. Sockets");
    Console.WriteLine("3. WebSockets");
    do
    { 
        string? input = Console.ReadLine();

        if (input == "1")
        {
            protocol = ServerType.gRPC;
        }
        else if (input == "2")
        {
            protocol = ServerType.Sockets;
        }
        else if (input == "3")
        {
            protocol = ServerType.WebSockets;
        }
        else if (input == "0")
        {
            Environment.Exit(0);
            return ServerType.None;
        }
        else
        {
            Console.WriteLine("Invalid selection. Please try again.");
            continue;
        }

        return protocol;
    } while (true);
}


enum ServerType
{
    None,
    gRPC,
    Sockets,
    WebSockets
};
