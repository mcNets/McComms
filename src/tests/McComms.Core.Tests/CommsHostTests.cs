using NUnit.Framework;
using McComms.Core;

namespace McComms.Core.Tests;

[TestFixture]
public class CommsHostTests
{
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string expectedHost = "localhost";
        const int expectedPort = 5000;

        // Act
        var commsHost = new CommsHost(expectedHost, expectedPort);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(commsHost.Host, Is.EqualTo(expectedHost));
            Assert.That(commsHost.Port, Is.EqualTo(expectedPort));
        });
    }

    [Test]
    public void ImplementsICommsHostInterface()
    {
        // Arrange & Act
        var commsHost = new CommsHost("localhost", 5000);

        // Assert
        Assert.That(commsHost, Is.InstanceOf<CommsHost>());
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var host1 = new CommsHost("localhost", 5000);
        var host2 = new CommsHost("localhost", 5000);

        // Act & Assert
        Assert.That(host1, Is.EqualTo(host2));
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var host1 = new CommsHost("localhost", 5000);
        var host2 = new CommsHost("127.0.0.1", 5000);
        var host3 = new CommsHost("localhost", 6000);

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(host1, Is.Not.EqualTo(host2));
            Assert.That(host1, Is.Not.EqualTo(host3));
            Assert.That(host2, Is.Not.EqualTo(host3));
        });
    }

    [Test]
    public void WithMethods_UpdatePropertiesCorrectly()
    {
        // Arrange
        var originalHost = new CommsHost("localhost", 5000);
        
        // Act
        var updatedHost = originalHost with { Host = "127.0.0.1" };
        var updatedPort = originalHost with { Port = 6000 };
        
        // Assert
        Assert.Multiple(() =>
        {
            // Updated host has new Host but same Port
            Assert.That(updatedHost.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(updatedHost.Port, Is.EqualTo(5000));
            
            // Updated port has new Port but same Host
            Assert.That(updatedPort.Host, Is.EqualTo("localhost"));
            Assert.That(updatedPort.Port, Is.EqualTo(6000));
            
            // Original remains unchanged
            Assert.That(originalHost.Host, Is.EqualTo("localhost"));
            Assert.That(originalHost.Port, Is.EqualTo(5000));
        });
    }
}
