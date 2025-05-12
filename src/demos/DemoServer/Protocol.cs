using System;

namespace DemoServer;

internal class Protocol
{
    internal ProtocolType Protocol { get; set; } = ProtocolType.None;

    public Protocol()
    {
        Protocol = GetProtocol();
    }

    private ProtocolType GetProtocol()
    {
        Console.WriteLine("Please select a protocol:");
        Console.WriteLine("0. Quit");
        Console.WriteLine("1. gRPC");
        Console.WriteLine("2. Sockets");
        Console.WriteLine("3. WebSockets");

        while (Protocol == ProtocolType.None)
        {
            Console.Write("Enter your selection: ");

            string? input = Console.ReadLine();

            if (input == "0")
            {
                Environment.Exit(0);
                return ProtocolType.None;
            }

            Protocol = input switch
            {
                "1" => ProtocolType.gRPC,
                "2" => ProtocolType.Sockets,
                "3" => ProtocolType.WebSockets,
                _ => ProtocolType.None
            };

            if (string.IsNullOrEmpty(input) || Protocol == ProtocolType.None)
            {
                Console.WriteLine("Invalid selection. Please try again.");
                continue;
            }
        }

        return Protocol;
    }
}