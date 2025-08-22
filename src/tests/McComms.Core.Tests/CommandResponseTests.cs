namespace McComms.Core.Tests;

[TestFixture]
public class CommandResponseTests
{
    [Test]
    public void ToString_FormatsCorrectly()
    {
        var response = new CommandResponse(true, "ID1", "msg");
        Assert.That(response.ToString(), Is.EqualTo("True:ID1:msg"));
    }

    [Test]
    public void TryParseCommandResponse_ValidString_ParsesCorrectly()
    {
        var str = "True:ID1:hello world";
        var result = str.TryParseCommandResponse(out var response);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Success, Is.True);
            Assert.That(response.Id, Is.EqualTo("ID1"));
            Assert.That(response.Message, Is.EqualTo("hello world"));
        });
    }

    [Test]
    public void TryParseCommandResponse_InvalidString_ReturnsFalse()
    {
        var str = "invalidstring";
        var result = str.TryParseCommandResponse(out var response);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(response, Is.Null);
        });
    }

    [Test]
    public void TryParseCommandResponse_NullOrEmpty_ReturnsFalse()
    {
        string? str = null;
        var result = str.TryParseCommandResponse(out var response);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(response, Is.Null);
        });
        str = "";
        result = str.TryParseCommandResponse(out response);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(response, Is.Null);
        });
    }
}
