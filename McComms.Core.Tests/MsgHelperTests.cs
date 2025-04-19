using NUnit.Framework;
using McComms.Core;

namespace McComms.Core.Tests;

[TestFixture]
public class MsgHelperTests
{
    [Test]
    public void Ok_NoParams_ReturnsSuccessResponse()
    {
        var response = MsgHelper.Ok();
        Assert.Multiple(() => {
            Assert.That(response.Success, Is.True);
            Assert.That(response.Id, Is.EqualTo(string.Empty));
            Assert.That(response.Message, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Ok_WithMessage_ReturnsSuccessResponseWithMessage()
    {
        var response = MsgHelper.Ok("done");
        Assert.Multiple(() => {
            Assert.That(response.Success, Is.True);
            Assert.That(response.Id, Is.EqualTo(string.Empty));
            Assert.That(response.Message, Is.EqualTo("done"));
        });
    }

    [Test]
    public void Fail_NoParams_ReturnsFailureResponse()
    {
        var response = MsgHelper.Fail();
        Assert.Multiple(() => {
            Assert.That(response.Success, Is.False);
            Assert.That(response.Id, Is.EqualTo(string.Empty));
            Assert.That(response.Message, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Fail_WithParams_ReturnsFailureResponseWithIdAndMessage()
    {
        var response = MsgHelper.Fail("ERR01", "error message");
        Assert.Multiple(() => {
            Assert.That(response.Success, Is.False);
            Assert.That(response.Id, Is.EqualTo("ERR01"));
            Assert.That(response.Message, Is.EqualTo("error message"));
        });
    }
}
