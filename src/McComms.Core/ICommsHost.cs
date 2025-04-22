using System;

namespace McComms.Core;

public interface ICommsHost {
    string Host { get; set; }
    int Port { get; set; }
}
