namespace McComms.Core;

public record NetworkAddress(string Host, int Port) {
    public override string ToString() => $"{Host}:{Port}";
}