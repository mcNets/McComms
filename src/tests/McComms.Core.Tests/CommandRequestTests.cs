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
        var result = str.TryParseCommandRequest(out var request);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(request, Is.Not.Null);
            Assert.That(request!.Id, Is.EqualTo(2));
            Assert.That(request.Message, Is.EqualTo("hello world,p1:p2"));
        });
    }

    [Test]
    public void TryParseCommandRequest_InvalidString_ReturnsFalse()
    {
        var str = "invalidstring";
        var result = str.TryParseCommandRequest(out var request);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(request, Is.Null);
        });
    }

    [Test]
    public void TryParseCommandRequest_NullOrEmpty_ReturnsFalse()
    {
        string? str1 = null;
        var result1 = str1.TryParseCommandResponse(out var response1);
        var str2 = "";
        var result2 = str2.TryParseCommandResponse(out var response2);
        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.False);
            Assert.That(response1, Is.Null);
            Assert.That(result2, Is.False);
            Assert.That(response2, Is.Null);
        });
    }
}
