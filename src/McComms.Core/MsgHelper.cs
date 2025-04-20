namespace McComms.Core;

public class MsgHelper
{
    public static CommandResponse Ok() {
        return new CommandResponse(true, string.Empty, string.Empty);
    }

    public static CommandResponse Ok(string message) {
        return new CommandResponse(true, string.Empty, message);
    }

    public static CommandResponse Fail() {
        return new CommandResponse(false, string.Empty, string.Empty);
    }

    public static CommandResponse Fail(string id, string message) {
        return new CommandResponse(false, id, message);
    }
}
