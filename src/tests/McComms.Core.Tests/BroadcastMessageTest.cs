namespace McComms.Core.Tests;

[TestFixture]
public class BroadcastMessageTest
{
    [Test]
    public void ToString_FormatsCorrectly()
    {
        var response = new BroadcastMessage(1, "msg,p1,p2");
        Assert.That(response.ToString(), Is.EqualTo("1:msg,p1,p2"));
    }

    [Test]
    public void TryParseBroadcastMessage_ValidString_ParsesCorrectly()
    {
        var str = "1:broadcast";
        var result = str.TryParseBroadcastMessage(out var broadcast);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(broadcast, Is.Not.Null);
            Assert.That(broadcast!.Id, Is.EqualTo(1));
            Assert.That(broadcast.Message, Is.EqualTo("broadcast"));
        });
    }

    [Test]
    public void TryParseBroadcastMessage_InvalidString_ReturnsFalse()
    {
        var str = "invalidstring";
        var result = str.TryParseBroadcastMessage(out var broadcast);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(broadcast, Is.Null);
        });
    }

    [Test]
    public void TryParseBroadcastMessage_NullOrEmpty_ReturnsFalse()
    {
        string? str1 = null;
        var result1 = str1.TryParseBroadcastMessage(out var broadcast1);
        var str2 = "";
        var result2 = str2.TryParseBroadcastMessage(out var broadcast2);
        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.False);
            Assert.That(broadcast1, Is.Null);
            Assert.That(result2, Is.False);
            Assert.That(broadcast2, Is.Null);
        });
    }
}
