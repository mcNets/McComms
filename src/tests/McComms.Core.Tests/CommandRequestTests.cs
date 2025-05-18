using NUnit.Framework;
using McComms.Core;

namespace McComms.Core.Tests;

[TestFixture]
public class CommandRequestTests
{
    [Test]
    public void ToString_FormatsCorrectly()
    {
        var response = new CommandRequest(1, "msg,p1,p2");
        Assert.That(response.ToString(), Is.EqualTo("1:msg,p1,p2"));
    }

    [Test]
    public void TryParseCommandRequest_ValidString_ParsesCorrectly()
    {
        var str = "2:hello world,p1:p2";
        var result = str.TryParseCommandResponse(out var request);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(request, Is.Not.Null);
            Assert.That(request!.Success, Is.True);
            Assert.That(request.Id, Is.EqualTo("ID1"));
            Assert.That(request.Message, Is.EqualTo("hello world"));
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

    [Test]
    public void TryParseCommandReqauest_ValidString_ParsesCorrectly()
    {
        var str = "1:ID1:hello world";
        var result = str.TryParseCommandRequest(out var command);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(command, Is.Not.Null);
            Assert.That(command!.Id, Is.EqualTo(1));
            Assert.That(command.Message, Is.EqualTo("ID1:hello world"));
        });
    }
}
