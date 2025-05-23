namespace McComms.Core;

public record CommsHost(string Host, int Port) {
    public override string ToString() => $"{Host}:{Port}";
}