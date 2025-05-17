namespace McComms.Core;

public interface ICommsHost {
    string Host { get; init; }
    int Port { get; init; }
}
