namespace VwConnector.Tests;

[TestClass]
public class SymmetricCryptoKeyTests
{
    [TestMethod]
    public void Construct()
    {
        var lead = Random.Shared.GetBytes(32);
        var tail = Random.Shared.GetBytes(32);

        var dualKey = SymmetricCryptoKey.From(lead.Concat(tail).ToArray());
        dualKey.Type.Should().Be(EncryptionType.AesCbc256_HmacSha256);
        dualKey.EncKey.Should().Equal(lead);
        dualKey.AuthKey.Should().Equal(tail);

        var singleKey = SymmetricCryptoKey.From(lead);
        singleKey.Type.Should().Be(EncryptionType.AesCbc256);
        singleKey.EncKey.Should().Equal(lead);
        singleKey.AuthKey.Should().BeNull();
    }
}
