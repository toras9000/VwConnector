namespace VwConnector.Tests;

[TestClass]
public class EncryptedDataTests
{
    [TestMethod]
    public void AesCbc256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.AesCbc256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"0.{bin2.EncodeBase64()}|{bin1.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.AesCbc256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().Equal(bin2);
        decoded.MAC.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void AesCbc128_HmacSha256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.AesCbc128_HmacSha256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"1.{bin2.EncodeBase64()}|{bin1.EncodeBase64()}|{bin3.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.AesCbc128_HmacSha256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().Equal(bin2);
        decoded.MAC.Should().Equal(bin3);
    }

    [TestMethod]
    public void AesCbc256_HmacSha256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.AesCbc256_HmacSha256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"2.{bin2.EncodeBase64()}|{bin1.EncodeBase64()}|{bin3.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.AesCbc256_HmacSha256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().Equal(bin2);
        decoded.MAC.Should().Equal(bin3);
    }

    [TestMethod]
    public void Rsa2048_OaepSha256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.Rsa2048_OaepSha256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"3.{bin1.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.Rsa2048_OaepSha256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().BeNullOrEmpty();
        decoded.MAC.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void Rsa2048_OaepSha1()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.Rsa2048_OaepSha1, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"4.{bin1.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.Rsa2048_OaepSha1);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().BeNullOrEmpty();
        decoded.MAC.Should().BeNullOrEmpty();
    }

    [TestMethod]
    public void Rsa2048_OaepSha256_HmacSha256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.Rsa2048_OaepSha256_HmacSha256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"5.{bin1.EncodeBase64()}|{bin3.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.Rsa2048_OaepSha256_HmacSha256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().BeNullOrEmpty();
        decoded.MAC.Should().Equal(bin3);
    }

    [TestMethod]
    public void Rsa2048_OaepSha1_HmacSha256()
    {
        var bin1 = Random.Shared.GetBytes(32);
        var bin2 = Random.Shared.GetBytes(32);
        var bin3 = Random.Shared.GetBytes(32);
        var encrypted = new EncryptedData(EncryptionType.Rsa2048_OaepSha1_HmacSha256, Data: bin1, IV: bin2, MAC: bin3);
        var encoded = encrypted.BuildString();
        encoded.Should().Be($"6.{bin1.EncodeBase64()}|{bin3.EncodeBase64()}");
        var decoded = EncryptedData.Parse(encoded);
        decoded.Type.Should().Be(EncryptionType.Rsa2048_OaepSha1_HmacSha256);
        decoded.Data.Should().Equal(bin1);
        decoded.IV.Should().BeNullOrEmpty();
        decoded.MAC.Should().Equal(bin3);
    }

    [TestMethod]
    public void TryParse()
    {
        var encrypted = new EncryptedData(EncryptionType.Rsa2048_OaepSha1_HmacSha256, Data: Random.Shared.GetBytes(32), IV: Random.Shared.GetBytes(32), MAC: Random.Shared.GetBytes(32));
        var encoded = encrypted.BuildString();

        EncryptedData.TryParse(encoded, out _).Should().BeTrue();
        EncryptedData.TryParse("abc", out _).Should().BeFalse();
    }

}
