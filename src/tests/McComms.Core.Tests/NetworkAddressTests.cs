
namespace McComms.Core.Tests;

[TestFixture]
public class NetworkAddressTests
{
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string expectedHost = "localhost";
        const int expectedPort = 5000;

        // Act
        var networkAddress = new Core.NetworkAddress(expectedHost, expectedPort);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(networkAddress.Host, Is.EqualTo(expectedHost));
            Assert.That(networkAddress.Port, Is.EqualTo(expectedPort));
        });
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var netAddress1 = new Core.NetworkAddress("localhost", 5000);
        var netAddress2 = new Core.NetworkAddress("localhost", 5000);

        // Act & Assert
        Assert.That(netAddress1, Is.EqualTo(netAddress2));
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var netAddress1 = new Core.NetworkAddress("localhost", 5000);
        var netAddress2 = new Core.NetworkAddress("127.0.0.1", 5000);
        var netAddress3 = new Core.NetworkAddress("localhost", 6000);

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(netAddress1, Is.Not.EqualTo(netAddress2));
            Assert.That(netAddress1, Is.Not.EqualTo(netAddress3));
            Assert.That(netAddress2, Is.Not.EqualTo(netAddress3));
        });
    }

    [Test]
    public void WithMethods_UpdatePropertiesCorrectly()
    {
        // Arrange
        var originalAddress = new Core.NetworkAddress("localhost", 5000);
        
        // Act
        var updatedHost = originalAddress with { Host = "127.0.0.1" };
        var updatedPort = originalAddress with { Port = 6000 };
        
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
            Assert.That(originalAddress.Host, Is.EqualTo("localhost"));
            Assert.That(originalAddress.Port, Is.EqualTo(5000));
        });
    }
}
