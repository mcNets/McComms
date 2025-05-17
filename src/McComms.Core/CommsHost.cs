namespace McComms.Core;

public record CommsHost(string Host, int Port) : ICommsHost;